using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Hangfire.Dashboard;
using InfluxDB.Collector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.InfluxDB
{
    public class InfluxDbAlertingPublisher : PublisherBase
    {
        private readonly InfluxDbTransportSettings _influxDBTransportSettings;

        public InfluxDbAlertingPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _influxDBTransportSettings = (InfluxDbTransportSettings)alertTransportSettings;
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                var alert = this.HasToPublishAlert(report);

                if (alert)
                {
                    using var collector = new CollectorConfiguration()
                        .Tag.With("name", _healthCheck.Name)
                        .Batch.AtInterval(TimeSpan.FromSeconds(2))
                        .WriteTo.InfluxDB(_influxDBTransportSettings.Host, _influxDBTransportSettings.Database)
                        .CreateCollector();

                    var entry = report
                        .Entries
                        .FirstOrDefault(x => x.Key == this._healthCheck.Name);

                    collector.Write("health_check",
                        new Dictionary<string, object>
                        {
                            { "status", (int)entry.Value.Status },
                            { "error", entry.Value.Exception },
                            { "responsetime", entry.Value.Duration.Milliseconds }
                        }, new Dictionary<string, string>()
                        {
                            { "endpoint", _healthCheck.EndpointOrHost ?? _healthCheck.ConnectionString }
                        });
                }
            });


            return Task.CompletedTask;
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<InfluxDbValidationError>()
                .Requires(_influxDBTransportSettings.Host)
                .IsNotNullOrEmpty();

            Condition.WithExceptionOnFailure<InfluxDbValidationError>()
                .Requires(_influxDBTransportSettings.Database)
                .IsNotNullOrEmpty();
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
