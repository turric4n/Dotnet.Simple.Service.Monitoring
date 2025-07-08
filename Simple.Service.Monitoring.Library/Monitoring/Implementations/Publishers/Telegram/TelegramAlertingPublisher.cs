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
using Telegram.Bot;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Telegram
{
    public class TelegramAlertingPublisher : PublisherBase
    {
        private readonly TelegramTransportSettings _telegramTransportSettings;
        private readonly TelegramBotClient _telegramBot;

        public TelegramAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _telegramTransportSettings = (TelegramTransportSettings)alertTransportSettings;
            _telegramBot = new TelegramBotClient(_telegramTransportSettings.BotApiToken);
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendTelegramMessage(new HealthCheckData(ownedEntry.Value, ownedEntry.Key), cancellationToken);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    // Convert health entries to HealthCheckData objects
                    await SendTelegramMessage(new HealthCheckData(interceptedEntry.Value, interceptedEntry.Key), cancellationToken);
                }
            }
        }

        private async Task SendTelegramMessage(HealthCheckData healthCheckData, CancellationToken cancellationToken)
        {
            // Format status with emoji for better visibility
            var statusEmoji = healthCheckData.Status switch
            {
                (Models.HealthStatus)HealthStatus.Unhealthy => "❌",
                (Models.HealthStatus)HealthStatus.Degraded => "⚠️",
                (Models.HealthStatus)HealthStatus.Healthy => "✅",
                _ => "❓"
            };

            var currentStatus = $"{statusEmoji} [{healthCheckData.Status}]";
            
            // Create a detailed and well-formatted message
            var body = $"{currentStatus} *{healthCheckData.Name}*{Environment.NewLine}{Environment.NewLine}" +
                       $"🕒 *Triggered On:* {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}" +
                       $"💻 *Machine:* {healthCheckData.MachineName}{Environment.NewLine}" +
                       $"🔧 *Service Type:* {healthCheckData.ServiceType}{Environment.NewLine}" +
                       $"🔗 *Endpoint:* {healthCheckData.Tags.GetValueOrDefault("Endpoint", healthCheckData.Tags.GetValueOrDefault("Host", "Not specified"))}{Environment.NewLine}" +
                       $"⏱ *Duration:* {healthCheckData.Duration} ms{Environment.NewLine}" +
                       $"📊 *Status:* {healthCheckData.Status}{Environment.NewLine}";

            // Add detailed error information
            if (healthCheckData.Status == (Models.HealthStatus)HealthStatus.Unhealthy ||
                healthCheckData.Status == (Models.HealthStatus)HealthStatus.Degraded)
            {
                var errorDetails = string.IsNullOrEmpty(healthCheckData.CheckError) || healthCheckData.CheckError == "None" 
                    ? healthCheckData.Description 
                    : healthCheckData.CheckError;
                
                body += $"❗ *Error Details:* {errorDetails}{Environment.NewLine}{Environment.NewLine}";
            }
            else
            {
                body += $"📝 *Details:* {healthCheckData.Description}{Environment.NewLine}{Environment.NewLine}";
            }

            // Add additional tags if available
            if (healthCheckData.Tags.Count > 0)
            {
                body += $"📋 *Additional Information:*{Environment.NewLine}";
                
                foreach (var tag in healthCheckData.Tags)
                {
                    // Skip endpoint as it's already displayed above
                    if (tag.Key != "Endpoint" && tag.Key != "Host")
                    {
                        body += $"- {tag.Key}: {tag.Value}{Environment.NewLine}";
                    }
                }
            }
            
            // Add timestamp footer
            body += $"{Environment.NewLine}🔄 Last updated: {healthCheckData.LastUpdated:yyyy-MM-dd HH:mm:ss}";

            await _telegramBot.SendMessage(_telegramTransportSettings.ChatId, body, cancellationToken: cancellationToken);
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<TelegramAlertingValidationError>()
                .Requires(_telegramTransportSettings.BotApiToken)
                .IsNotNullOrEmpty();

            Condition.WithExceptionOnFailure<TelegramAlertingValidationError>()
                .Requires(_telegramTransportSettings.ChatId)
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
