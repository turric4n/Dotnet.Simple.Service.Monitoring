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
            var healthCheckName = entry.Key;
            var healthCheckEntry = entry.Value;

            var msg = new SlackMessage
            {
                Channel = _slackTransportSettings.Channel,
                Text = $"Alert Triggered : {healthCheckName} {Environment.NewLine}" +
                       $"Triggered On    : {DateTime.Now} {Environment.NewLine}" +
                       $"Service Type    : {_healthCheck.ServiceType} {Environment.NewLine}" +
                       $"Alert Endpoint  : {_healthCheck.EndpointOrHost} {Environment.NewLine}" +
                       $"Alert Status    : {healthCheckEntry.Status} {Environment.NewLine}" +
                       $"Alert Duration  : {healthCheckEntry.Duration.TotalMilliseconds}ms {Environment.NewLine}" +
                       $"Alert Details   : {healthCheckEntry.Description} {Environment.NewLine}" +
                       $"Alert Exception : {healthCheckEntry.Exception} {Environment.NewLine}",
                As_user = false,
                Username = _slackTransportSettings.Username
            };

            foreach (var extraData in healthCheckEntry.Data)
            {
                msg.Text += $"Alert Tags     : {extraData.Key} - {extraData.Value} {Environment.NewLine}";
            }

            await SlackMessageSender.SendMessageAsync(_slackTransportSettings.Token, msg);
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
