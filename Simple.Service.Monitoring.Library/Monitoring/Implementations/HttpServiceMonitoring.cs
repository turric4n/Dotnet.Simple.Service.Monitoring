using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class HttpServiceMonitoring : ServiceMonitoringBase
    {

        public HttpServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            foreach (var host in this.HealthCheck.EndpointOrHost.Split(','))
            {
                Condition.Requires(this.HealthCheck.HealthCheckConditions)
                    .IsNotNull();
                Condition.Requires(this.HealthCheck.HealthCheckConditions.HttpBehaviour)
                    .IsNotNull();
                Condition.Requires(this.HealthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedCode)
                    .IsGreaterThan(0);
                Condition
                    .WithExceptionOnFailure<MalformedUriException>()
                    .Requires(Uri.IsWellFormedUriString(host, UriKind.Absolute))
                    .IsTrue();
            }
        }

        protected internal override void SetMonitoring()
        {
            var endpoints = HealthCheck.EndpointOrHost.Split(',').Select(e => new Uri(e.Trim())).ToList();
            var timeoutMs = HealthCheck.HealthCheckConditions.HttpBehaviour.HttpTimeoutMs;
            var timeout = timeoutMs > 0 ? TimeSpan.FromMilliseconds(timeoutMs) : TimeSpan.FromSeconds(30);
            var expectedCode = HealthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedCode;
            var httpVerb = HealthCheck.HealthCheckConditions.HttpBehaviour.HttpVerb;

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new HttpHealthCheck(endpoints, timeout, expectedCode, httpVerb);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    /// <summary>
    /// Custom HTTP/URI health check implementation
    /// </summary>
    internal class HttpHealthCheck : IHealthCheck
    {
        private readonly List<Uri> _endpoints;
        private readonly TimeSpan _timeout;
        private readonly int _expectedStatusCode;
        private readonly HttpVerb _httpVerb;
        
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        public HttpHealthCheck(List<Uri> endpoints, TimeSpan timeout, int expectedStatusCode, HttpVerb httpVerb)
        {
            _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
            _timeout = timeout;
            _expectedStatusCode = expectedStatusCode;
            _httpVerb = httpVerb;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var failures = new List<string>();
            var successes = new List<string>();

            foreach (var endpoint in _endpoints)
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(_timeout);

                    using var request = new HttpRequestMessage(GetHttpMethod(_httpVerb), endpoint);
                    request.Headers.Add("User-Agent", "HealthChecks");

                    var response = await _httpClient.SendAsync(request, cts.Token);
                    var statusCode = (int)response.StatusCode;

                    if (statusCode == _expectedStatusCode)
                    {
                        successes.Add($"{endpoint} returned expected status code {statusCode}");
                    }
                    else
                    {
                        failures.Add($"{endpoint} returned {statusCode}, expected {_expectedStatusCode}");
                    }
                }
                catch (TaskCanceledException)
                {
                    failures.Add($"{endpoint} timed out after {_timeout.TotalMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    failures.Add($"{endpoint} failed: {ex.Message}");
                }
            }

            if (failures.Any())
            {
                var data = new Dictionary<string, object>
                {
                    { "Failures", failures },
                    { "Successes", successes }
                };
                return HealthCheckResult.Unhealthy($"HTTP health check failed for {failures.Count} of {_endpoints.Count} endpoints", null, data);
            }

            return HealthCheckResult.Healthy($"All {_endpoints.Count} endpoints returned expected status code {_expectedStatusCode}");
        }

        private static HttpMethod GetHttpMethod(HttpVerb verb)
        {
            return verb switch
            {
                HttpVerb.Get => HttpMethod.Get,
                HttpVerb.Post => HttpMethod.Post,
                HttpVerb.Put => HttpMethod.Put,
                HttpVerb.Delete => HttpMethod.Delete,
                _ => throw new ArgumentOutOfRangeException(nameof(verb))
            };
        }
    }
}
