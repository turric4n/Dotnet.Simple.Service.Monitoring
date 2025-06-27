using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
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

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var serializedReport = JsonConvert.SerializeObject(report);

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Simple.Service.Monitoring.WebhookPublisher");
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var content = new StringContent(serializedReport, System.Text.Encoding.UTF8, "application/json");

            switch (_webhookTransportSettings.HttpBehaviour.HttpVerb)
            {
                case HttpVerb.Get:
                    return _httpClient.GetAsync(_webhookTransportSettings.WebhookUrl, cancellationToken);
                case HttpVerb.Post:
                    return _httpClient.PostAsync(_webhookTransportSettings.WebhookUrl, content, cancellationToken);
                case HttpVerb.Put:
                    return _httpClient.PutAsync(_webhookTransportSettings.WebhookUrl, content, cancellationToken);
                case HttpVerb.Delete:
                    return _httpClient.DeleteAsync(_webhookTransportSettings.WebhookUrl, cancellationToken);
                default:
                    throw new ArgumentOutOfRangeException();
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
