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
            var endpoint = !string.IsNullOrEmpty(HealthCheck.ConnectionString) 
                ? HealthCheck.ConnectionString 
                : HealthCheck.EndpointOrHost;

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new ElasticsearchHealthCheck(endpoint);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }

    }

    /// <summary>
    /// Custom Elasticsearch health check implementation
    /// </summary>
    internal class ElasticsearchHealthCheck : IHealthCheck
    {
        private readonly string _endpoint;
        private static readonly object _lock = new object();
        private static ElasticsearchClient _sharedClient;
        private static string _lastEndpoint;

        public ElasticsearchHealthCheck(string endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        private ElasticsearchClient GetOrCreateClient()
        {
            lock (_lock)
            {
                if (_sharedClient == null || _lastEndpoint != _endpoint)
                {
                    var settings = new ElasticsearchClientSettings(new Uri(_endpoint))
                        .RequestTimeout(TimeSpan.FromSeconds(5));

                    _sharedClient = new ElasticsearchClient(settings);
                    _lastEndpoint = _endpoint;
                }
                return _sharedClient;
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetOrCreateClient();
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
