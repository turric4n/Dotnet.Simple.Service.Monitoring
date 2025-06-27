using CuttingEdge.Conditions;
using InfluxDB.Collector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
                var ownedEntry = this.GetOwnedEntry(report);

                var interceptedEntries = this.GetInterceptedEntries(report);

                var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

                if (ownedAlerting)
                {
                    SendEntryToInflux(ownedEntry);
                }

                foreach (var entry in interceptedEntries)
                {
                    if (this.IsOkToAlert(entry, false))
                    {
                        SendEntryToInflux(entry);
                    }
                }

            }, cancellationToken);

            return Task.CompletedTask;
        }

        private void SendEntryToInflux(KeyValuePair<string, HealthReportEntry> healthReportEntry)
        {
            using var collector = new CollectorConfiguration()
                .Tag.With("name", _healthCheck.Name)
                .Batch.AtInterval(TimeSpan.FromSeconds(2))
                .WriteTo.InfluxDB(_influxDBTransportSettings.Host, _influxDBTransportSettings.Database)
                .CreateCollector();


            var tags = new Dictionary<string, string>()
            {
                { "endpoint", _healthCheck.EndpointOrHost ?? _healthCheck.ConnectionString }
            };

            var fields = new Dictionary<string, object>()
            {
                { "status", (int)healthReportEntry.Value.Status },
                { "error", healthReportEntry.Value.Exception },
                { "responsetime", healthReportEntry.Value.Duration.Milliseconds },
                { "description", healthReportEntry.Value.Description },
            };

            foreach (var valueTag in healthReportEntry.Value.Data)
            {
                fields.Add(valueTag.Key, valueTag.Value);
            }

            collector.Write("health_check", fields, tags);
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
