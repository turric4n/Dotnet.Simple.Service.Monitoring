using CuttingEdge.Conditions;
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
using Amazon;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.CloudWatch
{
    public class CloudWatchAlertingPublisher : PublisherBase, IDisposable
    {
        private readonly CloudWatchTransportSettings _cloudWatchTransportSettings;
        private AmazonCloudWatchClient _cloudWatchClient;
        private bool _disposed = false;

        public CloudWatchAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _cloudWatchTransportSettings = (CloudWatchTransportSettings)alertTransportSettings;
        }

        private AmazonCloudWatchClient GetClient()
        {
            if (_cloudWatchClient != null) return _cloudWatchClient;

            var region = RegionEndpoint.GetBySystemName(_cloudWatchTransportSettings.Region ?? "us-east-1");

            if (!string.IsNullOrEmpty(_cloudWatchTransportSettings.AccessKey) &&
                !string.IsNullOrEmpty(_cloudWatchTransportSettings.SecretKey))
            {
                _cloudWatchClient = new AmazonCloudWatchClient(
                    _cloudWatchTransportSettings.AccessKey,
                    _cloudWatchTransportSettings.SecretKey,
                    region);
            }
            else
            {
                _cloudWatchClient = new AmazonCloudWatchClient(region);
            }

            return _cloudWatchClient;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await PutCloudWatchMetricsAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await PutCloudWatchMetricsAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task PutCloudWatchMetricsAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var client = GetClient();
                var namespaceName = _cloudWatchTransportSettings.Namespace ?? "HealthChecks";

                var dimensions = new List<Dimension>
                {
                    new Dimension { Name = "HealthCheckName", Value = healthCheckData.Name },
                    new Dimension { Name = "ServiceType", Value = healthCheckData.ServiceType.ToString() },
                    new Dimension { Name = "MachineName", Value = healthCheckData.MachineName }
                };

                double.TryParse(healthCheckData.Duration, out var durationVal);
                var metricData = new List<MetricDatum>
                {
                    new MetricDatum
                    {
                        MetricName = "HealthCheckStatus",
                        Value = (int)healthCheckData.Status,
                        Unit = StandardUnit.None,
                        Dimensions = dimensions
                    },
                    new MetricDatum
                    {
                        MetricName = "HealthCheckDuration",
                        Value = durationVal,
                        Unit = StandardUnit.Milliseconds,
                        Dimensions = dimensions
                    }
                };

                var request = new PutMetricDataRequest
                {
                    Namespace = namespaceName,
                    MetricData = metricData
                };

                await client.PutMetricDataAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish CloudWatch metrics: {ex.Message}");
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<CloudWatchValidationError>()
                .Requires(_cloudWatchTransportSettings.Region)
                .IsNotNullOrEmpty();
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
                _cloudWatchClient?.Dispose();
            }
            _disposed = true;
        }

        ~CloudWatchAlertingPublisher()
        {
            Dispose(false);
        }
    }
}
