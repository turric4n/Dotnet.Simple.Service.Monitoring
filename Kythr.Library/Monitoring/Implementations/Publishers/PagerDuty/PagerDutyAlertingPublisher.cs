using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kythr.Library.Monitoring.Implementations.Publishers.PagerDuty
{
    public class PagerDutyAlertingPublisher : PublisherBase
    {
        private readonly PagerDutyTransportSettings _pagerDutyTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string PagerDutyEventsApiUrl = "https://events.pagerduty.com/v2/enqueue";

        public PagerDutyAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _pagerDutyTransportSettings = (PagerDutyTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendPagerDutyEventAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendPagerDutyEventAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendPagerDutyEventAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var severity = _pagerDutyTransportSettings.Severity ?? MapSeverity(healthCheckData.Status);

                var payload = new
                {
                    routing_key = _pagerDutyTransportSettings.RoutingKey,
                    event_action = HealthFailed(entry.Value.Status) ? "trigger" : "resolve",
                    dedup_key = $"health-check-{healthCheckData.Name}",
                    payload = new
                    {
                        summary = $"Health Check Alert: {healthCheckData.Name} - {healthCheckData.Status}",
                        severity,
                        source = healthCheckData.MachineName,
                        component = healthCheckData.Name,
                        group = healthCheckData.ServiceType.ToString(),
                        @class = "health_check",
                        custom_details = new
                        {
                            status = healthCheckData.Status.ToString(),
                            service_type = healthCheckData.ServiceType.ToString(),
                            duration_ms = healthCheckData.Duration,
                            description = healthCheckData.Description,
                            triggered_on = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(PagerDutyEventsApiUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send PagerDuty event");
            }
        }

        private static string MapSeverity(Models.HealthStatus status)
        {
            return status switch
            {
                Models.HealthStatus.Unhealthy => "critical",
                Models.HealthStatus.Degraded => "warning",
                Models.HealthStatus.Healthy => "info",
                _ => "info"
            };
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<PagerDutyValidationError>()
                .Requires(_pagerDutyTransportSettings.RoutingKey)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });
        }
    }
}
