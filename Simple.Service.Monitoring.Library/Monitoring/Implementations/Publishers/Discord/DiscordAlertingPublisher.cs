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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Discord
{
    public class DiscordAlertingPublisher : PublisherBase
    {
        private readonly DiscordTransportSettings _discordTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public DiscordAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _discordTransportSettings = (DiscordTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendDiscordMessageAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendDiscordMessageAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendDiscordMessageAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var statusEmoji = GetStatusEmoji(healthCheckData.Status);
                var color = GetEmbedColor(healthCheckData.Status);

                var payload = new
                {
                    username = _discordTransportSettings.Username ?? "Health Monitor",
                    avatar_url = _discordTransportSettings.AvatarUrl,
                    embeds = new[]
                    {
                        new
                        {
                            title = $"{statusEmoji} Health Check Alert: {healthCheckData.Name}",
                            description = healthCheckData.Description ?? "Health check status changed",
                            color,
                            fields = new[]
                            {
                                new { name = "Status", value = healthCheckData.Status.ToString(), inline = true },
                                new { name = "Service Type", value = healthCheckData.ServiceType.ToString(), inline = true },
                                new { name = "Duration", value = $"{healthCheckData.Duration} ms", inline = true },
                                new { name = "Machine", value = healthCheckData.MachineName, inline = true },
                                new { name = "Triggered On", value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), inline = false }
                            },
                            timestamp = DateTime.UtcNow.ToString("o")
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_discordTransportSettings.WebhookUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send Discord alert: {ex.Message}");
            }
        }

        private static string GetStatusEmoji(Models.HealthStatus status)
        {
            return status switch
            {
                Models.HealthStatus.Unhealthy => "❌",
                Models.HealthStatus.Degraded => "⚠️",
                Models.HealthStatus.Healthy => "✅",
                _ => "❓"
            };
        }

        private static int GetEmbedColor(Models.HealthStatus status)
        {
            return status switch
            {
                Models.HealthStatus.Unhealthy => 0xFF0000,
                Models.HealthStatus.Degraded => 0xFFA500,
                Models.HealthStatus.Healthy => 0x00FF00,
                _ => 0x808080
            };
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<DiscordValidationError>()
                .Requires(_discordTransportSettings.WebhookUrl)
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
