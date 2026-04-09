using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Opsgenie
{
    public class OpsgenieAlertingPublisher : PublisherBase
    {
        private readonly OpsgenieTransportSettings _opsgenieTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string OpsgenieApiUrl = "https://api.opsgenie.com/v2/alerts";

        public OpsgenieAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _opsgenieTransportSettings = (OpsgenieTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendOpsgenieAlertAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendOpsgenieAlertAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendOpsgenieAlertAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var priority = _opsgenieTransportSettings.Priority ?? MapPriority(healthCheckData.Status);

                var payload = new
                {
                    message = $"Health Check Alert: {healthCheckData.Name} - {healthCheckData.Status}",
                    alias = $"health-check-{healthCheckData.Name}",
                    description = $"Status: {healthCheckData.Status}\n" +
                                  $"Service Type: {healthCheckData.ServiceType}\n" +
                                  $"Duration: {healthCheckData.Duration} ms\n" +
                                  $"Machine: {healthCheckData.MachineName}\n" +
                                  $"Description: {healthCheckData.Description}",
                    priority,
                    tags = new[] { "health-check", healthCheckData.ServiceType.ToString() },
                    details = new Dictionary<string, string>
                    {
                        { "status", healthCheckData.Status.ToString() },
                        { "serviceType", healthCheckData.ServiceType.ToString() },
                        { "duration_ms", healthCheckData.Duration.ToString() },
                        { "machineName", healthCheckData.MachineName },
                        { "triggeredOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, OpsgenieApiUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("GenieKey", _opsgenieTransportSettings.ApiKey);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send Opsgenie alert: {ex.Message}");
            }
        }

        private static string MapPriority(Models.HealthStatus status)
        {
            return status switch
            {
                Models.HealthStatus.Unhealthy => "P1",
                Models.HealthStatus.Degraded => "P3",
                Models.HealthStatus.Healthy => "P5",
                _ => "P3"
            };
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<OpsgenieValidationError>()
                .Requires(_opsgenieTransportSettings.ApiKey)
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
