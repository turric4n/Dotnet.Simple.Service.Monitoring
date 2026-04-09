using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class TcpServiceMonitoring : ServiceMonitoringBase
    {
        public TcpServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(HealthCheck.EndpointOrHost)
                .IsNotNullOrEmpty();

            Condition
                .Requires(HealthCheck.HealthCheckConditions.TcpBehaviour.Port)
                .IsGreaterThan(0)
                .IsLessThan(65536);
        }

        protected internal override void SetMonitoring()
        {
            var host = HealthCheck.EndpointOrHost;
            var port = HealthCheck.HealthCheckConditions.TcpBehaviour.Port;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.TcpBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new TcpHealthCheck(host, port, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class TcpHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly TimeSpan _timeout;

        public TcpHealthCheck(string host, int port, TimeSpan timeout)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            TcpClient client = null;
            try
            {
                client = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);

                var connectTask = client.ConnectAsync(_host, _port);
                var completedTask = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));
                if (completedTask != connectTask)
                {
                    throw new OperationCanceledException();
                }
                await connectTask;

                if (client.Connected)
                {
                    return HealthCheckResult.Healthy($"TCP connection to {_host}:{_port} successful");
                }

                return HealthCheckResult.Unhealthy($"TCP connection to {_host}:{_port} failed");
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"TCP connection to {_host}:{_port} timed out after {_timeout.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"TCP connection to {_host}:{_port} failed: {ex.Message}", ex);
            }
            finally
            {
                client?.Dispose();
            }
        }
    }
}
