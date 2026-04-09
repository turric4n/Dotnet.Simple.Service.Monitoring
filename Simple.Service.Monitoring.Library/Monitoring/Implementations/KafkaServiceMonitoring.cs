using CuttingEdge.Conditions;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class KafkaServiceMonitoring : ServiceMonitoringBase
    {
        public KafkaServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.EndpointOrHost)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var bootstrapServers = HealthCheck.EndpointOrHost;
            var topicName = HealthCheck.HealthCheckConditions.KafkaBehaviour.TopicName;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.KafkaBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new KafkaHealthCheck(bootstrapServers, topicName, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class KafkaHealthCheck : IHealthCheck
    {
        private readonly string _bootstrapServers;
        private readonly string _topicName;
        private readonly TimeSpan _timeout;

        public KafkaHealthCheck(string bootstrapServers, string topicName, TimeSpan timeout)
        {
            _bootstrapServers = bootstrapServers ?? throw new ArgumentNullException(nameof(bootstrapServers));
            _topicName = topicName;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var config = new AdminClientConfig
                {
                    BootstrapServers = _bootstrapServers,
                    SocketTimeoutMs = (int)_timeout.TotalMilliseconds
                };

                using var adminClient = new AdminClientBuilder(config).Build();
                var metadata = adminClient.GetMetadata(TimeSpan.FromMilliseconds(_timeout.TotalMilliseconds));

                if (metadata.Brokers == null || metadata.Brokers.Count == 0)
                {
                    return HealthCheckResult.Unhealthy("No Kafka brokers available");
                }

                if (!string.IsNullOrEmpty(_topicName))
                {
                    var topic = metadata.Topics.FirstOrDefault(t => t.Topic == _topicName);
                    if (topic == null)
                    {
                        return HealthCheckResult.Degraded($"Kafka cluster reachable ({metadata.Brokers.Count} brokers) but topic '{_topicName}' not found");
                    }
                }

                return await Task.FromResult(
                    HealthCheckResult.Healthy($"Kafka cluster healthy. {metadata.Brokers.Count} broker(s) available"));
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Kafka connection failed: {ex.Message}", ex);
            }
        }
    }
}
