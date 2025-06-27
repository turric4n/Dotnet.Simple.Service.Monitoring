using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.CustomNotificationService
{
    public class CustomNotificationAlertingPublisher : PublisherBase
    {
        private readonly CustomNotificationTransportSettings _customNotificationTransportSettings;

        public CustomNotificationAlertingPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _customNotificationTransportSettings = (CustomNotificationTransportSettings)alertTransportSettings;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                var ownedEntry = this.GetOwnedEntry(report);
                var interceptedEntries = this.GetInterceptedEntries(report);

                var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

                if (ownedAlerting)
                {
                    await SendCustomNotificationAsync(ownedEntry, cancellationToken);
                }

                foreach (var interceptedEntry in interceptedEntries)
                {
                    if (this.IsOkToAlert(interceptedEntry, true))
                    {
                        await SendCustomNotificationAsync(interceptedEntry, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in custom notification publisher: {ex.Message}");
                // Log but don't rethrow to avoid breaking health checks
            }
        }

        private async Task SendCustomNotificationAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var notificationEndpoint = new Uri(_customNotificationTransportSettings.BaseEndpoint);

                using var httpclient = new HttpClient();
                httpclient.BaseAddress = notificationEndpoint;

                var healthCheckName = entry.Key;
                var healthCheckEntry = entry.Value;

                var body = $"Alert Triggered : {healthCheckName} {Environment.NewLine}" +
                           $"Triggered On    : {DateTime.Now} {Environment.NewLine}" +
                           $"Service Type    : {_healthCheck.ServiceType} {Environment.NewLine}" +
                           $"Alert Endpoint  : {_healthCheck.EndpointOrHost ?? _healthCheck.ConnectionString} {Environment.NewLine}" +
                           $"Alert Status    : {healthCheckEntry.Status} {Environment.NewLine}" +
                           $"Alert Duration  : {healthCheckEntry.Duration.TotalMilliseconds}ms {Environment.NewLine}" +
                           $"Alert Details   : {healthCheckEntry.Description} {Environment.NewLine}";

                if (healthCheckEntry.Exception != null)
                {
                    body += $"Exception      : {healthCheckEntry.Exception.Message} {Environment.NewLine}";
                }

                // Add any additional data
                foreach (var dataItem in healthCheckEntry.Data)
                {
                    body += $"Data: {dataItem.Key} = {dataItem.Value} {Environment.NewLine}";
                }

                var msg = new Message()
                {
                    Environment = _customNotificationTransportSettings.Environment,
                    Msg = body,
                    ProjectName = _customNotificationTransportSettings.ProjectName
                };

                var content = JsonConvert.SerializeObject(msg);

                httpclient.DefaultRequestHeaders.Add("X-Api-Key", _customNotificationTransportSettings.ApiKey);

                using var httpContent = new StringContent(
                    content,
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await httpclient.PostAsync("/notification/notify", httpContent, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send notification: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending custom notification: {ex.Message}");
                // Log but don't rethrow
            }
        }

        protected internal override void Validate()
        {
            // Basic validation could be added here
            if (string.IsNullOrEmpty(_customNotificationTransportSettings.BaseEndpoint))
            {
                throw new ArgumentException("BaseEndpoint is required for custom notification");
            }
            
            if (string.IsNullOrEmpty(_customNotificationTransportSettings.ApiKey))
            {
                throw new ArgumentException("ApiKey is required for custom notification");
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
