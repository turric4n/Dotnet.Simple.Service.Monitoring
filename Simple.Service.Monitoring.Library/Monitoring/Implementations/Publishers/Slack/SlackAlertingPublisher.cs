using System;
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

            var alert = this.HasToPublishAlert(report);

            var entry = report
                .Entries
                .FirstOrDefault(x => x.Key == this._healthCheck.Name);

            if (alert)
            {
                var msg = new SlackMessage
                {
                    Channel = _slackTransportSettings.Channel,
                    Text =  $"Alert Triggered : {_healthCheck.Name} {Environment.NewLine}" +
                            $"Triggered On    : { DateTime.UtcNow } {Environment.NewLine}" +
                            $"Service Type    : {_healthCheck.ServiceType} {Environment.NewLine}" +
                            $"Alert Endpoint : {_healthCheck.EndpointOrHost} {Environment.NewLine}" +
                            $"Alert Status   : {entry.Value.Status} {Environment.NewLine}" +
                            $"Alert Details  : {entry.Value.Status} {Environment.NewLine}" +
                            $"Alert Details  : {entry.Value.Description} {Environment.NewLine}" +
                            $"Alert Details  : {entry.Value.Exception} {Environment.NewLine}",
                    As_user = false,
                    Username = _slackTransportSettings.Username
                };

                await SlackMessageSender.SendMessageAsync(_slackTransportSettings.Token, msg);
            }
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
