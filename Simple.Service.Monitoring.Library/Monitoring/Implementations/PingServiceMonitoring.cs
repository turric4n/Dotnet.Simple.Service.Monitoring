using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
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

            HealthChecksBuilder.AddCheck(
                HealthCheck.Name,
                new PingHealthCheck(hosts),
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                GetTags());
        }

    }

    /// <summary>
    /// Custom Ping health check implementation
    /// </summary>
    internal class PingHealthCheck : IHealthCheck
    {
        private readonly List<string> _hosts;
        private const int TimeoutMs = 1000;

        public PingHealthCheck(List<string> hosts)
        {
            _hosts = hosts ?? throw new ArgumentNullException(nameof(hosts));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var failures = new List<string>();
            var successes = new List<string>();

            foreach (var host in _hosts)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(host, TimeoutMs);

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
                }
            }

            if (failures.Any())
            {
                var data = new Dictionary<string, object>
                {
                    { "Failures", failures },
                    { "Successes", successes }
                };
                return HealthCheckResult.Unhealthy($"Ping failed for {failures.Count} of {_hosts.Count} hosts", null, data);
            }

            return HealthCheckResult.Healthy($"All {_hosts.Count} hosts responded successfully");
        }
    }
}
