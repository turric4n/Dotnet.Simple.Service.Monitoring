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

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Datadog
{
    public class DatadogAlertingPublisher : PublisherBase
    {
        private readonly DatadogTransportSettings _datadogTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public DatadogAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _datadogTransportSettings = (DatadogTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendDatadogEventAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendDatadogEventAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendDatadogEventAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var alertType = MapAlertType(healthCheckData.Status);
                var site = _datadogTransportSettings.Site ?? "datadoghq.com";
                var apiUrl = $"https://api.{site}/api/v1/events";

                var payload = new
                {
                    title = $"Health Check Alert: {healthCheckData.Name}",
                    text = $"Status: {healthCheckData.Status}\n" +
                           $"Service Type: {healthCheckData.ServiceType}\n" +
                           $"Duration: {healthCheckData.Duration} ms\n" +
                           $"Machine: {healthCheckData.MachineName}\n" +
                           $"Description: {healthCheckData.Description}",
                    alert_type = alertType,
                    source_type_name = "health_check",
                    tags = new[]
                    {
                        $"service:{healthCheckData.Name}",
                        $"service_type:{healthCheckData.ServiceType}",
                        $"status:{healthCheckData.Status}",
                        $"machine:{healthCheckData.MachineName}"
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("DD-API-KEY", _datadogTransportSettings.ApiKey);
                if (!string.IsNullOrEmpty(_datadogTransportSettings.ApplicationKey))
                {
                    request.Headers.Add("DD-APPLICATION-KEY", _datadogTransportSettings.ApplicationKey);
                }
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send Datadog event: {ex.Message}");
            }
        }

        private static string MapAlertType(Models.HealthStatus status)
        {
            return status switch
            {
                Models.HealthStatus.Unhealthy => "error",
                Models.HealthStatus.Degraded => "warning",
                Models.HealthStatus.Healthy => "success",
                _ => "info"
            };
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<DatadogValidationError>()
                .Requires(_datadogTransportSettings.ApiKey)
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
