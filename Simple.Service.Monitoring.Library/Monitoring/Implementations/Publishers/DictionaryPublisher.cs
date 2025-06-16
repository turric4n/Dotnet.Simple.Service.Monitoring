using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers
{
    public class DictionaryPublisher : PublisherBase
    {
        private ConcurrentDictionary<DateTime, HealthReport> _reportDictionary;
        public DictionaryPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _reportDictionary = new ConcurrentDictionary<DateTime, HealthReport>();
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var alert = this.HasToPublishAlert(report);

            if (alert)
            {
                _reportDictionary.AddOrUpdate(DateTime.Now, report, (time, healthReport) => report);
            }

            return Task.CompletedTask;
        }

        protected internal override void Validate()
        {
            return;
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
