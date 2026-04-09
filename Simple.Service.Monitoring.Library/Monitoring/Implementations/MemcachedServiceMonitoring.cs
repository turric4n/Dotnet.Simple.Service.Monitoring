using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class MemcachedServiceMonitoring : ServiceMonitoringBase
    {
        public MemcachedServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.EndpointOrHost)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var endpoint = HealthCheck.EndpointOrHost;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.MemcachedBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new MemcachedHealthCheck(endpoint, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class MemcachedHealthCheck : IHealthCheck
    {
        private readonly string _endpoint;
        private readonly TimeSpan _timeout;

        public MemcachedHealthCheck(string endpoint, TimeSpan timeout)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            TcpClient client = null;
            try
            {
                var parts = _endpoint.Split(':');
                var host = parts[0];
                var port = parts.Length > 1 && int.TryParse(parts[1], out var p) ? p : 11211;

                client = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);

                var connectTask = client.ConnectAsync(host, port);
                var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));
                if (completed != connectTask)
                {
                    throw new OperationCanceledException();
                }
                await connectTask;

                var stream = client.GetStream();
                var command = Encoding.ASCII.GetBytes("stats\r\n");
                await stream.WriteAsync(command, 0, command.Length, cts.Token);

                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                var response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                if (response.Contains("STAT"))
                {
                    // Send quit command
                    var quit = Encoding.ASCII.GetBytes("quit\r\n");
                    await stream.WriteAsync(quit, 0, quit.Length, cts.Token);

                    return HealthCheckResult.Healthy($"Memcached at {_endpoint} is responding");
                }

                return HealthCheckResult.Unhealthy($"Memcached at {_endpoint} returned unexpected response");
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"Memcached connection to {_endpoint} timed out after {_timeout.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Memcached connection to {_endpoint} failed: {ex.Message}", ex);
            }
            finally
            {
                client?.Dispose();
            }
        }
    }
}
