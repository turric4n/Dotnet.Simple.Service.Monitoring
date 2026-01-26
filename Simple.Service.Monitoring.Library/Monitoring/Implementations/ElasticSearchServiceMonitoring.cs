using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Net.Http;
using System.Text.Json;
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
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        public ElasticsearchHealthCheck(string endpoint)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var uri = _endpoint.TrimEnd('/') + "/_cluster/health";
                var response = await _httpClient.GetAsync(uri, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Unhealthy($"Elasticsearch returned status code: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;

                if (root.TryGetProperty("status", out var statusProperty))
                {
                    var status = statusProperty.GetString();
                    
                    return status switch
                    {
                        "green" => HealthCheckResult.Healthy($"Elasticsearch cluster is healthy (status: {status})"),
                        "yellow" => HealthCheckResult.Degraded($"Elasticsearch cluster is degraded (status: {status})"),
                        "red" => HealthCheckResult.Unhealthy($"Elasticsearch cluster is unhealthy (status: {status})"),
                        _ => HealthCheckResult.Unhealthy($"Elasticsearch cluster has unknown status: {status}")
                    };
                }

                return HealthCheckResult.Unhealthy("Elasticsearch cluster health response missing 'status' field");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Elasticsearch connection failed: {ex.Message}", ex);
            }
        }
    }
}
