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
using InfluxDB.Collector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Telegram.Bot;


namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Telegram
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

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var alert = this.HasToPublishAlert(report);

            if (!alert) return;

            var telegramBot = new TelegramBotClient(_telegramTransportSettings.BotApiToken);

            var entry = report
                .Entries
                .FirstOrDefault(x =>
                    x.Key == this._healthCheck.Name);

            var currentStatus = "[Undefined]";

            switch (entry.Value.Status)
            {
                case HealthStatus.Unhealthy:
                    currentStatus = "[Unhealthy]";
                    break;
                case HealthStatus.Degraded:
                    currentStatus = "[Degraded]";
                    break;
                case HealthStatus.Healthy:
                    currentStatus = "[Healthy]";
                    break;
            }

            var body = $"{currentStatus} - Alert Triggered : {_healthCheck.Name} {Environment.NewLine}" +
                       $"Triggered On    : {DateTime.Now} {Environment.NewLine}" +
                       $"Service Type    : {_healthCheck.ServiceType} {Environment.NewLine}" +
                       $"Alert Endpoint  : {_healthCheck.EndpointOrHost} {Environment.NewLine}" +
                       $"Alert Status    : {entry.Value.Status} {Environment.NewLine}" +
                       $"Alert Details   : {(string.IsNullOrEmpty(entry.Value.Description) ? entry.Value.Exception?.Message : entry.Value.Description) } {Environment.NewLine}";

            foreach (var extraData in entry.Value.Data)
            {
                body += $"Alert Tags    : {extraData.Key} - {extraData.Value} {Environment.NewLine}";
            }

            await telegramBot.SendMessage(_telegramTransportSettings.ChatId, body, cancellationToken: cancellationToken);
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
