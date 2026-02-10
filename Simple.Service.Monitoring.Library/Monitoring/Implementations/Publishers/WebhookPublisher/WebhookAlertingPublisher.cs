using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.WebhookPublisher
{
    public class WebhookAlertingPublisher : PublisherBase
    {
        private readonly WebhookTransportSettings _webhookTransportSettings;
        private readonly HttpClient _httpClient;

        public WebhookAlertingPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _webhookTransportSettings = (WebhookTransportSettings)alertTransportSettings;
            _httpClient = new HttpClient();
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await SendWebhookAsync(ownedEntry, cancellationToken);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await SendWebhookAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task SendWebhookAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var serializedData = JsonConvert.SerializeObject(healthCheckData, Formatting.Indented);

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Simple.Service.Monitoring.WebhookPublisher");

                var content = new StringContent(serializedData, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = _webhookTransportSettings.HttpBehaviour.HttpVerb switch
                {
                    HttpVerb.Get => await _httpClient.GetAsync(_webhookTransportSettings.WebhookUrl, cancellationToken),
                    HttpVerb.Post => await _httpClient.PostAsync(_webhookTransportSettings.WebhookUrl, content, cancellationToken),
                    HttpVerb.Put => await _httpClient.PutAsync(_webhookTransportSettings.WebhookUrl, content, cancellationToken),
                    HttpVerb.Delete => await _httpClient.DeleteAsync(_webhookTransportSettings.WebhookUrl, cancellationToken),
                    _ => throw new ArgumentOutOfRangeException()
                };

                response.EnsureSuccessStatusCode();

                // Notify observers after successful send
                AlertObservers(entry);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send webhook: {ex.Message}");
            }
        }

        protected internal override void Validate()
        {
            Condition.Requires(_webhookTransportSettings)
                .IsNotNull();
            Condition.Requires(_webhookTransportSettings.WebhookUrl)
                .IsNotNullOrWhiteSpace();
            Condition.Ensures(_webhookTransportSettings.WebhookUrl)
                .Evaluate(v => Uri.IsWellFormedUriString(v, UriKind.Absolute),
                    "The provided Webhook URL is malformed.");

            _httpClient.BaseAddress = new Uri(_webhookTransportSettings.WebhookUrl);
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
