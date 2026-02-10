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
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                
                GetReportLastCheck();

                var statusEmoji = healthCheckData.Status switch
                {
                    (Models.HealthStatus)HealthStatus.Unhealthy => "❌",
                    (Models.HealthStatus)HealthStatus.Degraded => "⚠️",
                    (Models.HealthStatus)HealthStatus.Healthy => "✅",
                    _ => "❓"
                };

                var currentStatus = $"{statusEmoji} [{healthCheckData.Status}]";
                var subject = $"{currentStatus} - Alert Triggered: {healthCheckData.Name}";

                var body = $"<h2>{currentStatus} - Alert Triggered: {healthCheckData.Name}</h2>" +
                          $"<table style='border-collapse: collapse; width: 100%;'>" +
                          $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>🕒 Triggered On:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td></tr>" +
                          $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>💻 Machine:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{healthCheckData.MachineName}</td></tr>" +
                          $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>🔧 Service Type:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{healthCheckData.ServiceType}</td></tr>" +
                          $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>🔗 Endpoint:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{healthCheckData.Tags.GetValueOrDefault("Endpoint", healthCheckData.Tags.GetValueOrDefault("Host", "Not specified"))}</td></tr>" +
                          $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>⏱️ Duration:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{healthCheckData.Duration} ms</td></tr>" +
                          $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>📊 Status:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{healthCheckData.Status}</td></tr>";

                // Add detailed error information
                if (healthCheckData.Status == (Models.HealthStatus)HealthStatus.Unhealthy ||
                    healthCheckData.Status == (Models.HealthStatus)HealthStatus.Degraded)
                {
                    var errorDetails = string.IsNullOrEmpty(healthCheckData.CheckError) || healthCheckData.CheckError == "None" 
                        ? healthCheckData.Description 
                        : healthCheckData.CheckError;
                    
                    body += $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>❗️ Error Details:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{errorDetails}</td></tr>";
                }
                else
                {
                    body += $"<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>📝 Details:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>{healthCheckData.Description}</td></tr>";
                }

                body += "</table>";

                // Add detailed failure/success information from HealthCheckResult.Data
                var failures = healthCheckData.Tags.GetValueOrDefault("Data_Failures");
                var successes = healthCheckData.Tags.GetValueOrDefault("Data_Successes");
                
                if (!string.IsNullOrEmpty(failures) || !string.IsNullOrEmpty(successes))
                {
                    body += "<br/><h3>📋 Detailed Results</h3>";
                    
                    if (!string.IsNullOrEmpty(failures))
                    {
                        var failureList = failures.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                        body += $"<h4>❌ Failed ({failureList.Length}):</h4><ul>";
                        foreach (var failure in failureList)
                        {
                            body += $"<li>{failure}</li>";
                        }
                        body += "</ul>";
                    }
                    
                    if (!string.IsNullOrEmpty(successes))
                    {
                        var successList = successes.Split(new[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                        body += $"<h4>✅ Succeeded ({successList.Length}):</h4><ul>";
                        foreach (var success in successList)
                        {
                            body += $"<li>{success}</li>";
                        }
                        body += "</ul>";
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
                    body += "<br/><h3>📋 Additional Information</h3><ul>";
                    foreach (var tag in additionalTags)
                    {
                        body += $"<li><strong>{tag.Key}:</strong> {tag.Value}</li>";
                    }
                    body += "</ul>";
                }

                body += $"<br/><p style='color: #666;'><strong>🔄 Last updated:</strong> {healthCheckData.LastUpdated:yyyy-MM-dd HH:mm:ss}</p>";

                body = StandardEmailTemplate.TemplateBody.Replace("#replace", body);

                var message = MailMessageFactory.Create(_emailTransportSettings.To, subject, body);
                _mailSenderClient.SendMessage(message);

                // Notify observers after successful send
                AlertObservers(entry);

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
