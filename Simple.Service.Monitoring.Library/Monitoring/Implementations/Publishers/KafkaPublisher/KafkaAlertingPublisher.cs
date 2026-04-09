using CuttingEdge.Conditions;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.KafkaPublisher
{
    public class KafkaAlertingPublisher : PublisherBase, IDisposable
    {
        private readonly KafkaTransportSettings _kafkaTransportSettings;
        private IProducer<string, string> _producer;
        private bool _disposed = false;

        public KafkaAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _kafkaTransportSettings = (KafkaTransportSettings)alertTransportSettings;
        }

        private IProducer<string, string> GetProducer()
        {
            if (_producer != null) return _producer;

            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaTransportSettings.BootstrapServers,
                ClientId = _kafkaTransportSettings.ClientId ?? "health-check-publisher",
                Acks = Acks.Leader
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            return _producer;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await PublishToKafkaAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await PublishToKafkaAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task PublishToKafkaAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var producer = GetProducer();
                var topic = _kafkaTransportSettings.Topic ?? "health-checks";

                var message = new
                {
                    timestamp = DateTime.UtcNow,
                    name = healthCheckData.Name,
                    status = healthCheckData.Status.ToString(),
                    statusCode = (int)healthCheckData.Status,
                    serviceType = healthCheckData.ServiceType.ToString(),
                    durationMs = healthCheckData.Duration,
                    machineName = healthCheckData.MachineName,
                    description = healthCheckData.Description,
                    error = healthCheckData.CheckError
                };

                var json = JsonConvert.SerializeObject(message);

                await producer.ProduceAsync(topic, new Message<string, string>
                {
                    Key = healthCheckData.Name,
                    Value = json
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish to Kafka: {ex.Message}");
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<KafkaTransportValidationError>()
                .Requires(_kafkaTransportSettings.BootstrapServers)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _producer?.Dispose();
            }
            _disposed = true;
        }

        ~KafkaAlertingPublisher()
        {
            Dispose(false);
        }
    }
}
