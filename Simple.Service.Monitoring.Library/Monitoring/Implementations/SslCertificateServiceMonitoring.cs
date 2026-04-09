using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class SslCertificateServiceMonitoring : ServiceMonitoringBase
    {
        public SslCertificateServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
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
            var port = HealthCheck.HealthCheckConditions.SslCertificateBehaviour.Port;
            var warningDays = HealthCheck.HealthCheckConditions.SslCertificateBehaviour.WarningDaysBeforeExpiry;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.SslCertificateBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new SslCertificateHealthCheck(host, port, warningDays, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class SslCertificateHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly int _port;
        private readonly int _warningDays;
        private readonly TimeSpan _timeout;

        public SslCertificateHealthCheck(string host, int port, int warningDays, TimeSpan timeout)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _port = port;
            _warningDays = warningDays;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            TcpClient client = null;
            SslStream sslStream = null;
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

                // Accept all certificates since the purpose of this monitor is to
                // inspect the certificate's expiry, not to enforce chain trust.
                // The retrieved certificate is still validated for expiry below.
                sslStream = new SslStream(client.GetStream(), false,
                    (sender, certificate, chain, errors) =>
                    {
                        // Only ignore chain-trust errors; still reject null certs
                        return certificate != null;
                    });

                await sslStream.AuthenticateAsClientAsync(_host);

                var cert = sslStream.RemoteCertificate as X509Certificate2
                    ?? new X509Certificate2(sslStream.RemoteCertificate.Export(X509ContentType.Cert));

                var expiryDate = cert.NotAfter;
                var daysUntilExpiry = (expiryDate - DateTime.UtcNow).Days;

                if (expiryDate <= DateTime.UtcNow)
                {
                    return HealthCheckResult.Unhealthy(
                        $"SSL certificate for {_host}:{_port} has expired on {expiryDate:yyyy-MM-dd}");
                }

                if (daysUntilExpiry <= _warningDays)
                {
                    return HealthCheckResult.Degraded(
                        $"SSL certificate for {_host}:{_port} expires in {daysUntilExpiry} days ({expiryDate:yyyy-MM-dd})");
                }

                return HealthCheckResult.Healthy(
                    $"SSL certificate for {_host}:{_port} is valid. Expires in {daysUntilExpiry} days ({expiryDate:yyyy-MM-dd}). Subject: {cert.Subject}");
            }
            catch (OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy($"SSL certificate check for {_host}:{_port} timed out");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"SSL certificate check for {_host}:{_port} failed: {ex.Message}", ex);
            }
            finally
            {
                sslStream?.Dispose();
                client?.Dispose();
            }
        }
    }
}
