using CuttingEdge.Conditions;
using Elastic.Clients.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class ElasticSearchServiceMonitoring : ServiceMonitoringBase
    {

        public ElasticSearchServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.IsWellFormedUriString(this.HealthCheck.EndpointOrHost, UriKind.Absolute))
                .IsTrue();
    }

        protected internal override void SetMonitoring()
        {
            HealthChecksBuilder.AddCheck(
                HealthCheck.Name,
                new ElasticsearchHealthCheck(HealthCheck.EndpointOrHost),
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                GetTags());
        }

    }

    /// <summary>
    /// Custom Elasticsearch health check implementation
    /// </summary>
    internal class ElasticsearchHealthCheck : IHealthCheck
    {
        private readonly string _endpoint;

        public ElasticsearchHealthCheck(string endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = new ElasticsearchClientSettings(new Uri(_endpoint))
                    .RequestTimeout(TimeSpan.FromSeconds(5));

                var client = new ElasticsearchClient(settings);
                var pingResponse = await client.PingAsync(cancellationToken);

                if (pingResponse.IsValidResponse)
                {
                    return HealthCheckResult.Healthy("Elasticsearch is healthy");
                }

                return HealthCheckResult.Unhealthy($"Elasticsearch ping failed");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Elasticsearch connection failed: {ex.Message}", ex);
            }
        }
    }
}
