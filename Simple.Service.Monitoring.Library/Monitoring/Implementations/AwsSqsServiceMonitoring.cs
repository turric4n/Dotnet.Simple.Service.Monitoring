using CuttingEdge.Conditions;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class AwsSqsServiceMonitoring : ServiceMonitoringBase
    {
        public AwsSqsServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.HealthCheckConditions.AwsSqsBehaviour.QueueUrl)
                .IsNotNullOrEmpty();

            Condition
                .Requires(HealthCheck.HealthCheckConditions.AwsSqsBehaviour.Region)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var behaviour = HealthCheck.HealthCheckConditions.AwsSqsBehaviour;
            var timeout = TimeSpan.FromMilliseconds(behaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new AwsSqsHealthCheck(behaviour, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class AwsSqsHealthCheck : IHealthCheck
    {
        private readonly AwsSqsBehaviour _behaviour;
        private readonly TimeSpan _timeout;

        public AwsSqsHealthCheck(AwsSqsBehaviour behaviour, TimeSpan timeout)
        {
            _behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var region = Amazon.RegionEndpoint.GetBySystemName(_behaviour.Region);
                AmazonSQSClient client;

                if (!string.IsNullOrEmpty(_behaviour.AccessKey) && !string.IsNullOrEmpty(_behaviour.SecretKey))
                {
                    client = new AmazonSQSClient(_behaviour.AccessKey, _behaviour.SecretKey, region);
                }
                else
                {
                    client = new AmazonSQSClient(region);
                }

                using (client)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_timeout);

                    var request = new GetQueueAttributesRequest
                    {
                        QueueUrl = _behaviour.QueueUrl,
                        AttributeNames = new List<string> { "ApproximateNumberOfMessages" }
                    };

                    var response = await client.GetQueueAttributesAsync(request, cts.Token);
                    var messageCount = response.ApproximateNumberOfMessages;

                    return HealthCheckResult.Healthy(
                        $"AWS SQS queue is accessible. Approximate message count: {messageCount}");
                }
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"AWS SQS health check failed: {ex.Message}", ex);
            }
        }
    }
}
