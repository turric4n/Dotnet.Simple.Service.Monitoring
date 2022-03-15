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
        public SmtpMailMessageFactory _mailMessageFactory { get; }
        private EmailTransportSettings _emailTransportSettings;

        public EmailAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _emailTransportSettings = (EmailTransportSettings)alertTransportSettings;
            _mailSenderClient = new SmtpMailSender(_emailTransportSettings);
            _mailMessageFactory = new SmtpMailMessageFactory(_emailTransportSettings);
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var alert = this.HasToPublishAlert(report);

            if (!alert) return Task.CompletedTask;

            var entry = report
                .Entries
                .FirstOrDefault(x => x.Key == this._healthCheck.Name);

            var lastchecktime = GetReportLastCheck(report.Status);

            var subject = $"Alert Triggered : {_healthCheck.Name} ";

            var body = $"Alert Triggered : {_healthCheck.Name} {Environment.NewLine}" +
                       $"Triggered On    : {DateTime.UtcNow} {Environment.NewLine}" +
                       $"Service Type    : {_healthCheck.ServiceType} {Environment.NewLine}" +
                       $"Alert Endpoint  : {_healthCheck.EndpointOrHost} {Environment.NewLine}" +
                       $"Alert Status    : {entry.Value.Status} {Environment.NewLine}" +
                       $"Alert Details   : {entry.Value.Description} {Environment.NewLine}";

            foreach (var extraData in entry.Value.Data)
            {
                body += $"Alert Tags    : {extraData.Key} - {extraData.Value} {Environment.NewLine}";
            }

            body = StandardEmailTemplate.TemplateBody.Replace("#replace", body);

            //Do work
            var message = _mailMessageFactory.Create(_emailTransportSettings.To, subject, body);

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
