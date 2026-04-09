using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Prometheus
{
    public class PrometheusAlertingPublisher : PublisherBase
    {
        private readonly PrometheusTransportSettings _prometheusTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public PrometheusAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _prometheusTransportSettings = (PrometheusTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await PushMetricsAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await PushMetricsAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task PushMetricsAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var jobName = _prometheusTransportSettings.JobName ?? "health_check";
                var sanitizedName = SanitizeMetricName(healthCheckData.Name);

                // Build Prometheus exposition format
                var sb = new StringBuilder();
                sb.AppendLine($"# HELP health_check_status Health check status (0=Healthy, 1=Degraded, 2=Unhealthy)");
                sb.AppendLine($"# TYPE health_check_status gauge");
                sb.AppendLine($"health_check_status{{name=\"{sanitizedName}\",service_type=\"{healthCheckData.ServiceType}\"}} {(int)healthCheckData.Status}");
                sb.AppendLine($"# HELP health_check_duration_ms Health check duration in milliseconds");
                sb.AppendLine($"# TYPE health_check_duration_ms gauge");
                sb.AppendLine($"health_check_duration_ms{{name=\"{sanitizedName}\",service_type=\"{healthCheckData.ServiceType}\"}} {healthCheckData.Duration}");

                var pushUrl = $"{_prometheusTransportSettings.PushgatewayUrl.TrimEnd('/')}/metrics/job/{jobName}/instance/{sanitizedName}";
                using var content = new StringContent(sb.ToString(), Encoding.UTF8, "text/plain");
                var response = await _httpClient.PostAsync(pushUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to push Prometheus metrics");
            }
        }

        private static string SanitizeMetricName(string name)
        {
            return name?.Replace(" ", "_").Replace("-", "_").Replace(".", "_") ?? "unknown";
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<PrometheusValidationError>()
                .Requires(_prometheusTransportSettings.PushgatewayUrl)
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
