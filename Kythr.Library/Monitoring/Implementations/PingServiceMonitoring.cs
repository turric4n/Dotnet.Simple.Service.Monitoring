using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class PingServiceMonitoring : ServiceMonitoringBase
    {

        public PingServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.CheckHostName(HealthCheck.EndpointOrHost))
                .IsNotEqualTo(UriHostNameType.Unknown);
        }

        protected internal override void SetMonitoring()
        {
            var hosts = HealthCheck.EndpointOrHost.Split(',').Select(h => h.Trim()).ToList();
            var timeoutMs = HealthCheck.HealthCheckConditions.PingBehaviour.TimeOutMs;

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new PingHealthCheck(hosts, timeoutMs);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }

    }

    /// <summary>
    /// Custom Ping health check implementation
    /// </summary>
    internal class PingHealthCheck : IHealthCheck
    {
        private readonly List<string> _hosts;
        private readonly int _timeoutMs;

        public PingHealthCheck(List<string> hosts, int timeoutMs = 5000)
        {
            _hosts = hosts ?? throw new ArgumentNullException(nameof(hosts));
            _timeoutMs = timeoutMs;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var failures = new List<string>();
            var successes = new List<string>();
            Exception lastException = null;

            foreach (var host in _hosts)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(host, _timeoutMs);

                    if (reply.Status == IPStatus.Success)
                    {
                        successes.Add($"{host} responded in {reply.RoundtripTime}ms");
                    }
                    else
                    {
                        failures.Add($"{host} returned status {reply.Status}");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"{host} failed: {ex.Message}");
                    lastException = ex;
                }
            }

            if (failures.Any())
            {
                var data = new Dictionary<string, object>
                {
                    { "Failures", failures },
                    { "Successes", successes }
                };
                return HealthCheckResult.Unhealthy(
                    $"Ping failed for {failures.Count} of {_hosts.Count} hosts", 
                    lastException, 
                    data);
            }

            return HealthCheckResult.Healthy($"All {_hosts.Count} hosts responded successfully");
        }
    }
}
