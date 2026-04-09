using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.RabbitMQ
{
    public class RmqAlertingPublisher : PublisherBase, IDisposable
    {
        private readonly RmqTransportSettings _rmqTransportSettings;
        private IConnection _connection;
        private IChannel _channel;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed = false;

        public RmqAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _rmqTransportSettings = (RmqTransportSettings)alertTransportSettings;
        }

        private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
        {
            if (_channel != null) return _channel;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                if (_channel != null) return _channel;

                var factory = new ConnectionFactory
                {
                    Uri = new Uri(_rmqTransportSettings.ConnectionString)
                };

                _connection = await factory.CreateConnectionAsync(cancellationToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

                if (!string.IsNullOrEmpty(_rmqTransportSettings.Exchange))
                {
                    await _channel.ExchangeDeclareAsync(
                        _rmqTransportSettings.Exchange,
                        ExchangeType.Topic,
                        durable: true,
                        cancellationToken: cancellationToken);
                }

                if (!string.IsNullOrEmpty(_rmqTransportSettings.QueueName))
                {
                    await _channel.QueueDeclareAsync(
                        _rmqTransportSettings.QueueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        cancellationToken: cancellationToken);

                    if (!string.IsNullOrEmpty(_rmqTransportSettings.Exchange))
                    {
                        await _channel.QueueBindAsync(
                            _rmqTransportSettings.QueueName,
                            _rmqTransportSettings.Exchange,
                            _rmqTransportSettings.RoutingKey ?? "health.check.#",
                            cancellationToken: cancellationToken);
                    }
                }

                return _channel;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await PublishToRabbitMQAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await PublishToRabbitMQAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task PublishToRabbitMQAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var channel = await GetChannelAsync(cancellationToken);

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
                var body = Encoding.UTF8.GetBytes(json);

                var routingKey = _rmqTransportSettings.RoutingKey ??
                    $"health.check.{healthCheckData.Name.Replace(" ", ".").ToLowerInvariant()}";

                var exchange = _rmqTransportSettings.Exchange ?? "";

                await channel.BasicPublishAsync(
                    exchange,
                    routingKey,
                    body,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish to RabbitMQ: {ex.Message}");
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<RmqTransportValidationError>()
                .Requires(_rmqTransportSettings.ConnectionString)
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
                _channel?.Dispose();
                _connection?.Dispose();
                _semaphore?.Dispose();
            }
            _disposed = true;
        }

        ~RmqAlertingPublisher()
        {
            Dispose(false);
        }
    }
}
