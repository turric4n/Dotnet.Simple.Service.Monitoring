using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Telegram.Bot;

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
            var alert = this.HasToPublishAlert(report);

            if (alert)
            {
                var notificationEndpoint = new Uri(_customNotificationTransportSettings.BaseEndpoint);

                using var httpclient = new HttpClient();
                httpclient.BaseAddress = notificationEndpoint;

                var entry = report
                    .Entries
                    .FirstOrDefault(x => x.Key == this._healthCheck.Name);

                var body = $"Alert Triggered : {_healthCheck.Name} {Environment.NewLine}" +
                           $"Triggered On    : {DateTime.Now} {Environment.NewLine}" +
                           $"Service Type    : {_healthCheck.ServiceType} {Environment.NewLine}" +
                           $"Alert Endpoint : {_healthCheck.EndpointOrHost} {Environment.NewLine}" +
                           $"Alert Status   : {entry.Value.Status} {Environment.NewLine}" +
                           $"Alert Details  : {entry.Value.Description} {Environment.NewLine}";

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

                var response = await httpclient.PostAsync("/notification/notify", httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        "failed to send message."
                    );
                }
            }
        }

        protected internal override void Validate()
        {
            //
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
