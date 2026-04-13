using CuttingEdge.Conditions;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class AzureServiceBusServiceMonitoring : ServiceMonitoringBase
    {
        public AzureServiceBusServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.ConnectionString)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var connectionString = HealthCheck.ConnectionString;
            var queueOrTopicName = HealthCheck.HealthCheckConditions.AzureServiceBusBehaviour.QueueOrTopicName;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.AzureServiceBusBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new AzureServiceBusHealthCheck(connectionString, queueOrTopicName, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class AzureServiceBusHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _queueOrTopicName;
        private readonly TimeSpan _timeout;

        public AzureServiceBusHealthCheck(string connectionString, string queueOrTopicName, TimeSpan timeout)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _queueOrTopicName = queueOrTopicName;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var adminClient = new ServiceBusAdministrationClient(_connectionString);

                if (!string.IsNullOrEmpty(_queueOrTopicName))
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_timeout);

                    var queueExists = await adminClient.QueueExistsAsync(_queueOrTopicName, cts.Token);
                    if (queueExists.Value)
                    {
                        var properties = await adminClient.GetQueueRuntimePropertiesAsync(_queueOrTopicName, cts.Token);
                        return HealthCheckResult.Healthy(
                            $"Azure Service Bus queue '{_queueOrTopicName}' is accessible. Active messages: {properties.Value.ActiveMessageCount}");
                    }

                    var topicExists = await adminClient.TopicExistsAsync(_queueOrTopicName, cts.Token);
                    if (topicExists.Value)
                    {
                        var properties = await adminClient.GetTopicRuntimePropertiesAsync(_queueOrTopicName, cts.Token);
                        return HealthCheckResult.Healthy(
                            $"Azure Service Bus topic '{_queueOrTopicName}' is accessible. Subscription count: {properties.Value.SubscriptionCount}");
                    }

                    return HealthCheckResult.Unhealthy($"Azure Service Bus queue/topic '{_queueOrTopicName}' not found");
                }

                // Just verify the namespace is accessible
                await foreach (var queue in adminClient.GetQueuesAsync(cancellationToken))
                {
                    return HealthCheckResult.Healthy("Azure Service Bus namespace is accessible");
                }

                return HealthCheckResult.Healthy("Azure Service Bus namespace is accessible (no queues found)");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Azure Service Bus connection failed: {ex.Message}", ex);
            }
        }
    }
}
