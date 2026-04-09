using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class SmtpServiceMonitoring : ServiceMonitoringBase
    {
        public SmtpServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
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
            var host = HealthCheck.EndpointOrHost;
            var port = HealthCheck.HealthCheckConditions.SmtpBehaviour.Port;
            var useTls = HealthCheck.HealthCheckConditions.SmtpBehaviour.UseTls;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.SmtpBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new SmtpHealthCheck(host, port, useTls, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class SmtpHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly bool _useTls;
        private readonly TimeSpan _timeout;

        public SmtpHealthCheck(string host, int port, bool useTls, TimeSpan timeout)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _useTls = useTls;
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
                var completed = await Task.WhenAny(connectTask, Task.Delay(Timeout.Infinite, cts.Token));
                if (completed != connectTask)
                {
                    throw new OperationCanceledException();
                }
                await connectTask;

                Stream stream = client.GetStream();

                if (_useTls)
                {
                    var sslStream = new SslStream(stream, false);
                    await sslStream.AuthenticateAsClientAsync(_host);
                    stream = sslStream;
                }

                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, true);
                using var writer = new StreamWriter(stream, Encoding.ASCII, 1024, true) { AutoFlush = true };

                var banner = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(banner) || !banner.StartsWith("220"))
                {
                    return HealthCheckResult.Unhealthy($"SMTP server {_host}:{_port} returned unexpected banner: {banner}");
                }

                await writer.WriteLineAsync("EHLO healthcheck");
                var ehloResponse = await reader.ReadLineAsync();

                await writer.WriteLineAsync("QUIT");

                return HealthCheckResult.Healthy($"SMTP server {_host}:{_port} is responding. Banner: {banner}");
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"SMTP connection to {_host}:{_port} timed out after {_timeout.TotalMilliseconds}ms");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"SMTP connection to {_host}:{_port} failed: {ex.Message}", ex);
            }
            finally
            {
                client?.Dispose();
            }
        }
    }
}
