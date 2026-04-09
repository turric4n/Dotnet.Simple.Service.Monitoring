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

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.GoogleChat
{
    public class GoogleChatAlertingPublisher : PublisherBase
    {
        private readonly GoogleChatTransportSettings _googleChatTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public GoogleChatAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _googleChatTransportSettings = (GoogleChatTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendGoogleChatMessageAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendGoogleChatMessageAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendGoogleChatMessageAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var statusEmoji = healthCheckData.Status switch
                {
                    Models.HealthStatus.Unhealthy => "❌",
                    Models.HealthStatus.Degraded => "⚠️",
                    Models.HealthStatus.Healthy => "✅",
                    _ => "❓"
                };

                var text = $"{statusEmoji} *Health Check Alert: {healthCheckData.Name}*\n\n" +
                           $"*Status:* {healthCheckData.Status}\n" +
                           $"*Service Type:* {healthCheckData.ServiceType}\n" +
                           $"*Duration:* {healthCheckData.Duration} ms\n" +
                           $"*Machine:* {healthCheckData.MachineName}\n" +
                           $"*Triggered On:* {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                           $"*Description:* {healthCheckData.Description ?? "N/A"}";

                var payload = new { text };
                var json = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_googleChatTransportSettings.WebhookUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send Google Chat alert");
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<GoogleChatValidationError>()
                .Requires(_googleChatTransportSettings.WebhookUrl)
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
