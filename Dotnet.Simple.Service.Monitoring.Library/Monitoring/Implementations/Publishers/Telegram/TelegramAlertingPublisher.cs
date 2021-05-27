using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using InfluxDB.Collector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Telegram.Bot;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Telegram
{
    public class TelegramAlertingPublisher : PublisherBase
    {
        private readonly TelegramTransportSettings _telegramTransportSettings;

        public TelegramAlertingPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _telegramTransportSettings = (TelegramTransportSettings)alertTransportSettings;
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {

            var alert = this.HasToPublishAlert(report);

            if (alert)
            {
                var telegramBot = new TelegramBotClient(_telegramTransportSettings.BotApiToken);

                var entry = report.Entries.FirstOrDefault();

                var subject = $"Alert Triggered : {_healthCheck.Name} ";

                var body = $"Alert Triggered : {_healthCheck.Name} {Environment.NewLine}" +
                           $"Triggered On    : {DateTime.UtcNow} {Environment.NewLine}" +
                           $"Service Type    : {_healthCheck.ServiceType.ToString()} {Environment.NewLine}" +
                           $"Alert Endpoint : {_healthCheck.EndpointOrHost} {Environment.NewLine}" +
                           $"Alert Status   : {entry.Value.Status.ToString()} {Environment.NewLine}" +
                           $"Alert Details  : {entry.Value.Status.ToString()} {Environment.NewLine}" +
                           $"Alert Details  : {entry.Value.Description} {Environment.NewLine}" +
                           $"Alert Details  : {entry.Value.Exception?.ToString()} {Environment.NewLine}";

                telegramBot.SendTextMessageAsync(_telegramTransportSettings.ChatId, body);
            }

            return Task.CompletedTask;
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
