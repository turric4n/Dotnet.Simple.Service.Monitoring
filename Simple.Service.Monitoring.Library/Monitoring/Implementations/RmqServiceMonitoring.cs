using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class RmqServiceMonitoring : ServiceMonitoringBase
    {
        public RmqServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            var hasEndpointOrHost = !string.IsNullOrEmpty(HealthCheck.EndpointOrHost);
            var hasConnectionString = !string.IsNullOrEmpty(HealthCheck.ConnectionString);

            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(hasEndpointOrHost || hasConnectionString)
                .IsTrue("Either EndpointOrHost or ConnectionString must be provided");

            var connectionString = hasConnectionString ? HealthCheck.ConnectionString : HealthCheck.EndpointOrHost;

            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.IsWellFormedUriString(connectionString, UriKind.Absolute))
                .IsTrue();
        }

        protected internal override void SetMonitoring()
        {
            var connectionString = !string.IsNullOrEmpty(HealthCheck.ConnectionString) 
                ? HealthCheck.ConnectionString 
                : HealthCheck.EndpointOrHost;

            // Create a custom health check that GUARANTEES connection disposal using AddAsyncCheck
            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new RabbitMqConnectionHealthCheck(connectionString);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    /// <summary>
    /// Custom RabbitMQ health check with connection reuse
    /// </summary>
    internal class RabbitMqConnectionHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private static readonly object _lock = new object();
        private static IConnection _sharedConnection;
        private static string _lastConnectionString;

        public RabbitMqConnectionHealthCheck(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private async Task<IConnection> GetOrCreateConnectionAsync(CancellationToken cancellationToken)
        {
            IConnection connection = null;
            
            lock (_lock)
            {
                if (_sharedConnection == null || !_sharedConnection.IsOpen || _lastConnectionString != _connectionString)
                {
                    _sharedConnection?.Dispose();
                    connection = null;
                }
                else
                {
                    return _sharedConnection;
                }
            }

            // Create connection outside lock to avoid blocking
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_connectionString),
                AutomaticRecoveryEnabled = true,
                RequestedHeartbeat = TimeSpan.FromSeconds(10),
                ContinuationTimeout = TimeSpan.FromSeconds(10),
                HandshakeContinuationTimeout = TimeSpan.FromSeconds(10)
            };

            connection = await factory.CreateConnectionAsync(cancellationToken);

            lock (_lock)
            {
                _sharedConnection = connection;
                _lastConnectionString = _connectionString;
                return _sharedConnection;
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await GetOrCreateConnectionAsync(cancellationToken);

                return connection.IsOpen 
                    ? HealthCheckResult.Healthy($"RabbitMQ connection is healthy") 
                    : HealthCheckResult.Unhealthy($"RabbitMQ connection is not open");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"RabbitMQ connection failed: {ex.Message}", ex);
            }
        }
    }
}
