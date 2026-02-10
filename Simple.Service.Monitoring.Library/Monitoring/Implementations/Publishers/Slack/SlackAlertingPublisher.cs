using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Models;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Sender;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MsHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack
{
    public class SlackAlertingPublisher : PublisherBase
    {
        private readonly SlackTransportSettings _slackTransportSettings;

        public SlackAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
                ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _slackTransportSettings = (SlackTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);

            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendSlackMessageAsync(ownedEntry, cancellationToken);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendSlackMessageAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendSlackMessageAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            var healthCheckData = new HealthCheckData(entry.Value, entry.Key);

            var statusEmoji = healthCheckData.Status switch
            {
                Library.Models.HealthStatus.Unhealthy => ":x:",
                Library.Models.HealthStatus.Degraded => ":warning:",
                Library.Models.HealthStatus.Healthy => ":white_check_mark:",
                _ => ":question:"
            };

            var currentStatus = $"{statusEmoji} [{healthCheckData.Status}]";

            var text = $"*{currentStatus} - Alert Triggered: {healthCheckData.Name}*\n\n" +
                      $":clock2: *Triggered On:* {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
                      $":computer: *Machine:* {healthCheckData.MachineName}\n" +
                      $":wrench: *Service Type:* {healthCheckData.ServiceType}\n" +
                      $":link: *Endpoint:* {healthCheckData.Tags.GetValueOrDefault("Endpoint", healthCheckData.Tags.GetValueOrDefault("Host", "Not specified"))}\n" +
                      $":stopwatch: *Duration:* {healthCheckData.Duration} ms\n" +
                      $":bar_chart: *Status:* {healthCheckData.Status}\n";

            // Add detailed error information
            if (healthCheckData.Status == Library.Models.HealthStatus.Unhealthy ||
                healthCheckData.Status == Library.Models.HealthStatus.Degraded)
            {
                var errorDetails = string.IsNullOrEmpty(healthCheckData.CheckError) || healthCheckData.CheckError == "None" 
                    ? healthCheckData.Description 
                    : healthCheckData.CheckError;
                
                text += $"\n:exclamation: *Error Details:* {errorDetails}\n";
            }
            else
            {
                text += $"\n:memo: *Details:* {healthCheckData.Description}\n";
            }

            // Add detailed failure/success information from HealthCheckResult.Data
            var failures = healthCheckData.Tags.GetValueOrDefault("Data_Failures");
            var successes = healthCheckData.Tags.GetValueOrDefault("Data_Successes");
            
            if (!string.IsNullOrEmpty(failures) || !string.IsNullOrEmpty(successes))
            {
                text += "\n:clipboard: *Detailed Results:*\n";
                
                if (!string.IsNullOrEmpty(failures))
                {
                    var failureList = failures.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                    text += $"\n:x: *Failed ({failureList.Length}):*\n";
                    foreach (var failure in failureList)
                    {
                        text += $"  • {failure}\n";
                    }
                }
                
                if (!string.IsNullOrEmpty(successes))
                {
                    var successList = successes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                    text += $"\n:white_check_mark: *Succeeded ({successList.Length}):*\n";
                    foreach (var success in successList)
                    {
                        text += $"  • {success}\n";
                    }
                }
            }

            // Add additional tags if available
            var excludedTags = new HashSet<string> 
            { 
                "Endpoint", "Host", "ServiceType", 
                "Data_Failures", "Data_Successes"
            };
            
            var additionalTags = healthCheckData.Tags
                .Where(t => !excludedTags.Contains(t.Key))
                .ToList();
            
            if (additionalTags.Any())
            {
                text += "\n:clipboard: *Additional Information:*\n";
                foreach (var tag in additionalTags)
                {
                    text += $"  • {tag.Key}: {tag.Value}\n";
                }
            }

            text += $"\n:arrows_counterclockwise: *Last updated:* {healthCheckData.LastUpdated:yyyy-MM-dd HH:mm:ss}";

            var msg = new SlackMessage
            {
                Channel = _slackTransportSettings.Channel,
                Text = text,
                As_user = false,
                Username = _slackTransportSettings.Username
            };

            await SlackMessageSender.SendMessageAsync(_slackTransportSettings.Token, msg);
            
            // Notify observers after successful send
            AlertObservers(entry);
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<SlackAlertingValidationError>()
                .Requires(_slackTransportSettings.Token)
                .IsNotNullOrEmpty();

            Condition.WithExceptionOnFailure<SlackAlertingValidationError>()
                .Requires(_slackTransportSettings.Channel)
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
