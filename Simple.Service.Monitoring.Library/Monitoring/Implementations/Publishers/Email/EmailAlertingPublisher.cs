using System;
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

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var alert = this.HasToPublishAlert(report);

            if (!alert) return Task.CompletedTask;

            var entry = report
                .Entries
                .FirstOrDefault(x => x.Key == this._healthCheck.Name);

            GetReportLastCheck(report.Status);

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


            var subject = $"{currentStatus} - Alert Triggered : {_healthCheck.Name} ";

            var body = $"{currentStatus} - Alert Triggered : {_healthCheck.Name} <br>" +
                       $"Triggered On    : {DateTime.Now} <br>" +
                       $"Service Type    : {_healthCheck.ServiceType} <br>" +
                       $"Alert Endpoint  : {_healthCheck.EndpointOrHost} <br>" +
                       $"Alert Status    : {entry.Value.Status} <br>" +
                       $"Alert Details   : {entry.Value.Description} <br>";

            foreach (var extraData in entry.Value.Data)
            {
                body += $"Alert Tags    : {extraData.Key} - {extraData.Value} <br>";
            }

            body = StandardEmailTemplate.TemplateBody.Replace("#replace", body);

            //Do work
            var message = MailMessageFactory.Create(_emailTransportSettings.To, subject, body);

            _mailSenderClient.SendMessage(message);

            return Task.CompletedTask;
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
