using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Mattermost
{
    public class MattermostAlertingPublisher : PublisherBase
    {
        private readonly MattermostTransportSettings _mattermostTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public MattermostAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _mattermostTransportSettings = (MattermostTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendMattermostMessageAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendMattermostMessageAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendMattermostMessageAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var statusEmoji = healthCheckData.Status switch
                {
                    Models.HealthStatus.Unhealthy => ":x:",
                    Models.HealthStatus.Degraded => ":warning:",
                    Models.HealthStatus.Healthy => ":white_check_mark:",
                    _ => ":question:"
                };

                var text = $"{statusEmoji} **Health Check Alert: {healthCheckData.Name}**\n\n" +
                           $"| Property | Value |\n" +
                           $"|----------|-------|\n" +
                           $"| Status | {healthCheckData.Status} |\n" +
                           $"| Service Type | {healthCheckData.ServiceType} |\n" +
                           $"| Duration | {healthCheckData.Duration} ms |\n" +
                           $"| Machine | {healthCheckData.MachineName} |\n" +
                           $"| Triggered On | {DateTime.Now:yyyy-MM-dd HH:mm:ss} |\n" +
                           $"| Description | {healthCheckData.Description ?? "N/A"} |";

                var payload = new
                {
                    channel = _mattermostTransportSettings.Channel,
                    username = _mattermostTransportSettings.Username ?? "Health Monitor",
                    icon_url = _mattermostTransportSettings.IconUrl,
                    text
                };

                var json = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_mattermostTransportSettings.WebhookUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send Mattermost alert");
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<MattermostValidationError>()
                .Requires(_mattermostTransportSettings.WebhookUrl)
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
