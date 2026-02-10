using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
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
                // Notify observers after successful send
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    // Convert health entries to HealthCheckData objects
                    try
                    {
                        await SendTelegramMessage(new HealthCheckData(interceptedEntry.Value, interceptedEntry.Key), cancellationToken);
                        // Notify observers after successful send
                        AlertObservers(interceptedEntry);
                    }
                    catch (Exception e)
                    {
                        // Do something with the exception, e.g., log it
                    }
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
            
            // Create a detailed and well-formatted message using HTML formatting
            var body = $"{currentStatus} <b>{healthCheckData.Name}</b>\n\n" +
                       $"🕒 <b>Triggered On:</b> {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                       $"💻 <b>Machine:</b> {healthCheckData.MachineName}\n" +
                       $"🔧 <b>Service Type:</b> {healthCheckData.ServiceType}\n" +
                       $"🔗 <b>Endpoint:</b> {healthCheckData.Tags.GetValueOrDefault("Endpoint", healthCheckData.Tags.GetValueOrDefault("Host", "Not specified"))}\n" +
                       $"⏱️ <b>Duration:</b> {healthCheckData.Duration} ms\n" +
                       $"📊 <b>Status:</b> {healthCheckData.Status}\n";

            // Add detailed error information
            if (healthCheckData.Status == (Models.HealthStatus)HealthStatus.Unhealthy ||
                healthCheckData.Status == (Models.HealthStatus)HealthStatus.Degraded)
            {
                var errorDetails = string.IsNullOrEmpty(healthCheckData.CheckError) || healthCheckData.CheckError == "None" 
                    ? healthCheckData.Description 
                    : healthCheckData.CheckError;
                
                body += $"\n❗️ <b>Error Details:</b> {errorDetails}\n";
                
                // Add detailed failure/success information from HealthCheckResult.Data
                var failures = healthCheckData.Tags.GetValueOrDefault("Data_Failures");
                var successes = healthCheckData.Tags.GetValueOrDefault("Data_Successes");
                
                if (!string.IsNullOrEmpty(failures) || !string.IsNullOrEmpty(successes))
                {
                    body += $"\n📋 <b>Detailed Results:</b>\n";
                    
                    if (!string.IsNullOrEmpty(failures))
                    {
                        var failureList = failures.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                        body += $"\n❌ <b>Failed ({failureList.Length}):</b>\n";
                        foreach (var failure in failureList)
                        {
                            body += $"  • {failure}\n";
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(successes))
                    {
                        var successList = successes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                        body += $"\n✅ <b>Succeeded ({successList.Length}):</b>\n";
                        foreach (var success in successList)
                        {
                            body += $"  • {success}\n";
                        }
                    }
                }
            }
            else
            {
                body += $"\n📝 <b>Details:</b> {healthCheckData.Description}\n";
            }

            // Add additional tags if available
            if (healthCheckData.Tags.Count > 0)
            {
                var excludedTags = new HashSet<string> 
                { 
                    "Endpoint", "Host", "ServiceType", 
                    "Data_Failures", "Data_Successes" // Exclude these as they're shown above
                };
                
                var additionalTags = healthCheckData.Tags
                    .Where(t => !excludedTags.Contains(t.Key))
                    .ToList();
                
                if (additionalTags.Any())
                {
                    body += $"\n📋 <b>Additional Information:</b>\n";
                    
                    foreach (var tag in additionalTags)
                    {
                        body += $"  • {tag.Key}: {tag.Value}\n";
                    }
                }
            }
            
            // Add timestamp footer
            body += $"\n🔄 <b>Last updated:</b> {healthCheckData.LastUpdated:yyyy-MM-dd HH:mm:ss}";

            await _telegramBot.SendMessage(
                _telegramTransportSettings.ChatId, 
                body, 
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
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
