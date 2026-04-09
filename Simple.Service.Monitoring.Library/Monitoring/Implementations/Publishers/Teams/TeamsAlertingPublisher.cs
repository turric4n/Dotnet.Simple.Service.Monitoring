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

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Teams
{
    public class TeamsAlertingPublisher : PublisherBase
    {
        private readonly TeamsTransportSettings _teamsTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public TeamsAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _teamsTransportSettings = (TeamsTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendTeamsMessageAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendTeamsMessageAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendTeamsMessageAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var statusEmoji = GetStatusEmoji(healthCheckData.Status);
                var themeColor = GetThemeColor(healthCheckData.Status);

                var payload = new
                {
                    @type = "MessageCard",
                    context = "http://schema.org/extensions",
                    themeColor,
                    summary = $"{statusEmoji} Alert: {healthCheckData.Name}",
                    sections = new[]
                    {
                        new
                        {
                            activityTitle = $"{statusEmoji} Health Check Alert: {healthCheckData.Name}",
                            activitySubtitle = $"Status: {healthCheckData.Status}",
                            facts = new[]
                            {
                                new { name = "Service Type", value = healthCheckData.ServiceType.ToString() },
                                new { name = "Status", value = healthCheckData.Status.ToString() },
                                new { name = "Duration", value = $"{healthCheckData.Duration} ms" },
                                new { name = "Machine", value = healthCheckData.MachineName },
                                new { name = "Triggered On", value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                                new { name = "Description", value = healthCheckData.Description ?? "N/A" }
                            },
                            markdown = true
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_teamsTransportSettings.WebhookUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send Teams alert");
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

        private static string GetThemeColor(Models.HealthStatus status)
        {
            return status switch
            {
                Models.HealthStatus.Unhealthy => "FF0000",
                Models.HealthStatus.Degraded => "FFA500",
                Models.HealthStatus.Healthy => "00FF00",
                _ => "808080"
            };
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<TeamsValidationError>()
                .Requires(_teamsTransportSettings.WebhookUrl)
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
