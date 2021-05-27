using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Models;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Sender;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack
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

            var entry = report.Entries.FirstOrDefault();

            if (alert)
            {
                var msg = new SlackMessage
                {
                    Channel = _slackTransportSettings.Channel,
                    Text =  $"Alert Triggered : {_healthCheck.Name} {Environment.NewLine}" +
                            $"Triggered On    : {DateTime.UtcNow} {Environment.NewLine}" +
                            $"Service Type    : {_healthCheck.ServiceType.ToString()} {Environment.NewLine}" +
                            $"Alert Endpoint : {_healthCheck.EndpointOrHost} {Environment.NewLine}" +
                            $"Alert Status   : {entry.Value.Status.ToString()} {Environment.NewLine}" +
                            $"Alert Details  : {entry.Value.Status.ToString()} {Environment.NewLine}" +
                            $"Alert Details  : {entry.Value.Description} {Environment.NewLine}" +
                            $"Alert Details  : {entry.Value.Exception?.ToString()} {Environment.NewLine}",
                    As_user = false,
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
