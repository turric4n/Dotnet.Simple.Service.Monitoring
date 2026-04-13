using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class DnsServiceMonitoring : ServiceMonitoringBase
    {
        public DnsServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(HealthCheck.EndpointOrHost)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var hostname = HealthCheck.EndpointOrHost;
            var expectedIp = HealthCheck.HealthCheckConditions.DnsBehaviour.ExpectedIpAddress;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.DnsBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new DnsHealthCheck(hostname, expectedIp, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class DnsHealthCheck : IHealthCheck
    {
        private readonly string _hostname;
        private readonly string _expectedIp;
        private readonly TimeSpan _timeout;

        public DnsHealthCheck(string hostname, string expectedIp, TimeSpan timeout)
        {
            _hostname = hostname ?? throw new ArgumentNullException(nameof(hostname));
            _expectedIp = expectedIp;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);

                var dnsTask = Dns.GetHostAddressesAsync(_hostname);
                var completedTask = await Task.WhenAny(dnsTask, Task.Delay(Timeout.Infinite, cts.Token));
                if (completedTask != dnsTask)
                {
                    throw new OperationCanceledException();
                }
                var addresses = await dnsTask;

                if (addresses == null || addresses.Length == 0)
                {
                    return HealthCheckResult.Unhealthy($"DNS resolution for '{_hostname}' returned no addresses");
                }

                var resolvedIps = string.Join(", ", addresses.Select(a => a.ToString()));

                if (!string.IsNullOrEmpty(_expectedIp))
                {
                    var expectedAddress = IPAddress.Parse(_expectedIp);
                    if (!addresses.Contains(expectedAddress))
                    {
                        return HealthCheckResult.Unhealthy(
                            $"DNS resolution for '{_hostname}' resolved to [{resolvedIps}], expected {_expectedIp}");
                    }
                }

                return HealthCheckResult.Healthy($"DNS resolution for '{_hostname}' successful: [{resolvedIps}]");
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"DNS resolution for '{_hostname}' timed out after {_timeout.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"DNS resolution for '{_hostname}' failed: {ex.Message}", ex);
            }
        }
    }
}
