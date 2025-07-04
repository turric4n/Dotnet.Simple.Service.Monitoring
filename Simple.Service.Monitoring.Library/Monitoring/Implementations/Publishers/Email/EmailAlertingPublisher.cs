using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Templates;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email
{
    public class EmailAlertingPublisher : PublisherBase
    {
        private readonly IMailSenderClient _mailSenderClient;
        public SmtpMailMessageFactory MailMessageFactory { get; }
        private readonly EmailTransportSettings _emailTransportSettings;

        public EmailAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _emailTransportSettings = (EmailTransportSettings)alertTransportSettings;
            _mailSenderClient = new SmtpMailSender(_emailTransportSettings);
            MailMessageFactory = new SmtpMailMessageFactory(_emailTransportSettings);
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);
            
            if (ownedAlerting)
            {
                await SendEmailAlertAsync(ownedEntry, cancellationToken);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendEmailAlertAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private Task SendEmailAlertAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckName = entry.Key;
                var healthCheckEntry = entry.Value;

                GetReportLastCheck(healthCheckEntry.Status);

                var currentStatus = healthCheckEntry.Status switch
                {
                    HealthStatus.Unhealthy => "[Unhealthy]",
                    HealthStatus.Degraded => "[Degraded]",
                    HealthStatus.Healthy => "[Healthy]",
                    _ => "[Undefined]"
                };

                var subject = $"{currentStatus} - Alert Triggered : {healthCheckName}";

                var body = $"{currentStatus} - Alert Triggered : {healthCheckName} <br>" +
                          $"Triggered On    : {DateTime.Now} <br>" +
                          $"Service Type    : {_healthCheck.ServiceType} <br>" +
                          $"Alert Endpoint  : {_healthCheck.EndpointOrHost} <br>" +
                          $"Alert Status    : {healthCheckEntry.Status} <br>" +
                          $"Alert Duration  : {healthCheckEntry.Duration.TotalMilliseconds}ms <br>" +
                          $"Alert Details   : {healthCheckEntry.Description} <br>";

                if (healthCheckEntry.Exception != null)
                {
                    body += $"Exception      : {healthCheckEntry.Exception.Message} <br>";
                }

                foreach (var extraData in healthCheckEntry.Data)
                {
                    body += $"Alert Tags    : {extraData.Key} - {extraData.Value} <br>";
                }

                body = StandardEmailTemplate.TemplateBody.Replace("#replace", body);

                var message = MailMessageFactory.Create(_emailTransportSettings.To, subject, body);
                _mailSenderClient.SendMessage(message);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log exception but don't throw to avoid breaking health checks
                System.Diagnostics.Debug.WriteLine($"Failed to send email alert: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<EmailAlertingValidationError>()
                .Requires(new MailAddress(_emailTransportSettings.From))
                .IsNotNull();
            Condition.WithExceptionOnFailure<EmailAlertingValidationError>()
                .Requires(Uri.CheckHostName(_emailTransportSettings.SmtpHost))
                .IsNotEqualTo(UriHostNameType.Unknown);
            Condition.WithExceptionOnFailure<EmailAlertingValidationError>()
                .Requires(_emailTransportSettings.To)
                .IsNotNull();

            foreach (var mailto in _emailTransportSettings.To.Split(','))
            {
                Condition.Requires(new MailAddress(mailto))
                    .IsNotNull();
            }

            if (_emailTransportSettings.Authentication)
            {
                Condition.WithExceptionOnFailure<EmailAlertingValidationError>()
                    .Requires(_emailTransportSettings.Username)
                    .IsNotNull();
                Condition.WithExceptionOnFailure<EmailAlertingValidationError>()
                    .Requires(_emailTransportSettings.Password)
                    .IsNotNull();
            }
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
