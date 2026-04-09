using CuttingEdge.Conditions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.AppInsights
{
    public class AppInsightsAlertingPublisher : PublisherBase, IDisposable
    {
        private readonly AppInsightsTransportSettings _appInsightsTransportSettings;
        private TelemetryClient _telemetryClient;
        private TelemetryConfiguration _telemetryConfiguration;
        private bool _disposed = false;

        public AppInsightsAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _appInsightsTransportSettings = (AppInsightsTransportSettings)alertTransportSettings;
        }

        private TelemetryClient GetTelemetryClient()
        {
            if (_telemetryClient != null) return _telemetryClient;

            if (!string.IsNullOrEmpty(_appInsightsTransportSettings.ConnectionString))
            {
                _telemetryConfiguration = new TelemetryConfiguration
                {
                    ConnectionString = _appInsightsTransportSettings.ConnectionString
                };
            }
            else if (!string.IsNullOrEmpty(_appInsightsTransportSettings.InstrumentationKey))
            {
#pragma warning disable CS0618 // InstrumentationKey is deprecated but supported for backward compat
                _telemetryConfiguration = new TelemetryConfiguration(_appInsightsTransportSettings.InstrumentationKey);
#pragma warning restore CS0618
            }
            else
            {
                _telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            }

            _telemetryClient = new TelemetryClient(_telemetryConfiguration);
            return _telemetryClient;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                TrackHealthCheckEvent(ownedEntry);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    TrackHealthCheckEvent(interceptedEntry);
                }
            }

            await Task.CompletedTask;
        }

        private void TrackHealthCheckEvent(KeyValuePair<string, HealthReportEntry> entry)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var client = GetTelemetryClient();

                // Track as custom event
                var eventTelemetry = new EventTelemetry("HealthCheckAlert")
                {
                    Timestamp = DateTimeOffset.Now
                };
                eventTelemetry.Properties["HealthCheckName"] = healthCheckData.Name;
                eventTelemetry.Properties["Status"] = healthCheckData.Status.ToString();
                eventTelemetry.Properties["ServiceType"] = healthCheckData.ServiceType.ToString();
                eventTelemetry.Properties["MachineName"] = healthCheckData.MachineName;
                eventTelemetry.Properties["Description"] = healthCheckData.Description;
                if (double.TryParse(healthCheckData.Duration, out var durationMs))
                {
                    eventTelemetry.Metrics["DurationMs"] = durationMs;
                }
                eventTelemetry.Metrics["StatusCode"] = (int)healthCheckData.Status;

                client.TrackEvent(eventTelemetry);

                // Track availability
                double.TryParse(healthCheckData.Duration, out var durationVal);
                var availabilityTelemetry = new AvailabilityTelemetry
                {
                    Name = healthCheckData.Name,
                    Timestamp = DateTimeOffset.Now,
                    Duration = TimeSpan.FromMilliseconds(durationVal),
                    Success = !HealthFailed(entry.Value.Status),
                    RunLocation = healthCheckData.MachineName,
                    Message = healthCheckData.Description
                };
                availabilityTelemetry.Properties["ServiceType"] = healthCheckData.ServiceType.ToString();

                client.TrackAvailability(availabilityTelemetry);
                client.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send Application Insights telemetry: {ex.Message}");
            }
        }

        protected internal override void Validate()
        {
            var hasConnectionString = !string.IsNullOrEmpty(_appInsightsTransportSettings.ConnectionString);
            var hasInstrumentationKey = !string.IsNullOrEmpty(_appInsightsTransportSettings.InstrumentationKey);

            Condition.WithExceptionOnFailure<AppInsightsValidationError>()
                .Requires(hasConnectionString || hasInstrumentationKey)
                .IsTrue("Either ConnectionString or InstrumentationKey must be provided");
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _telemetryConfiguration?.Dispose();
            }
            _disposed = true;
        }

        ~AppInsightsAlertingPublisher()
        {
            Dispose(false);
        }
    }
}
