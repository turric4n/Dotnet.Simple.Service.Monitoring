using CuttingEdge.Conditions;
using FluentFTP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Renci.SshNet;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class FtpServiceMonitoring : ServiceMonitoringBase
    {
        public FtpServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
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
            var behaviour = HealthCheck.HealthCheckConditions.FtpBehaviour;
            var timeout = TimeSpan.FromMilliseconds(behaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new FtpHealthCheck(host, behaviour, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class FtpHealthCheck : IHealthCheck
    {
        private readonly string _host;
        private readonly FtpBehaviour _behaviour;
        private readonly TimeSpan _timeout;

        public FtpHealthCheck(string host, FtpBehaviour behaviour, TimeSpan timeout)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_behaviour.UseSftp)
            {
                return await CheckSftpAsync();
            }
            return await CheckFtpAsync(cancellationToken);
        }

        private async Task<HealthCheckResult> CheckFtpAsync(CancellationToken cancellationToken)
        {
            AsyncFtpClient client = null;
            try
            {
                client = new AsyncFtpClient(_host, _behaviour.Username, _behaviour.Password, _behaviour.Port);
                client.Config.ConnectTimeout = (int)_timeout.TotalMilliseconds;

                await client.Connect(cancellationToken);

                if (client.IsConnected)
                {
                    return HealthCheckResult.Healthy($"FTP connection to {_host}:{_behaviour.Port} successful");
                }

                return HealthCheckResult.Unhealthy($"FTP connection to {_host}:{_behaviour.Port} failed");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"FTP connection to {_host}:{_behaviour.Port} failed: {ex.Message}", ex);
            }
            finally
            {
                if (client != null)
                {
                    await client.Disconnect();
                    client.Dispose();
                }
            }
        }

        private Task<HealthCheckResult> CheckSftpAsync()
        {
            SftpClient client = null;
            try
            {
                client = new SftpClient(_host, _behaviour.Port == 21 ? 22 : _behaviour.Port,
                    _behaviour.Username ?? string.Empty,
                    _behaviour.Password ?? string.Empty);
                client.ConnectionInfo.Timeout = _timeout;

                client.Connect();

                if (client.IsConnected)
                {
                    return Task.FromResult(
                        HealthCheckResult.Healthy($"SFTP connection to {_host} successful"));
                }

                return Task.FromResult(
                    HealthCheckResult.Unhealthy($"SFTP connection to {_host} failed"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy($"SFTP connection to {_host} failed: {ex.Message}", ex));
            }
            finally
            {
                client?.Dispose();
            }
        }
    }
}
