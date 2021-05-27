using System;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email
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
