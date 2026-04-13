using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Kythr.Library.Monitoring.Implementations.Publishers.WebhookPublisher
{
    public class WebhookAlertingPublisher : PublisherBase
    {
        private readonly WebhookTransportSettings _webhookTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();

        public WebhookAlertingPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _webhookTransportSettings = (WebhookTransportSettings)alertTransportSettings;
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

                var content = new StringContent(serializedData, System.Text.Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage();
                request.RequestUri = new Uri(_webhookTransportSettings.WebhookUrl);
                request.Headers.Add("User-Agent", "Kythr.WebhookPublisher");

                // Apply user-configured custom headers
                if (_webhookTransportSettings.Headers != null)
                {
                    foreach (var header in _webhookTransportSettings.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                request.Method = _webhookTransportSettings.HttpBehaviour.HttpVerb switch
                {
                    HttpVerb.Get => HttpMethod.Get,
                    HttpVerb.Post => HttpMethod.Post,
                    HttpVerb.Put => HttpMethod.Put,
                    HttpVerb.Delete => HttpMethod.Delete,
                    _ => throw new ArgumentOutOfRangeException()
                };

                if (request.Method != HttpMethod.Get)
                {
                    request.Content = content;
                }

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                // Notify observers after successful send
                AlertObservers(entry);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to send webhook to {Url}", _webhookTransportSettings.WebhookUrl);
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
