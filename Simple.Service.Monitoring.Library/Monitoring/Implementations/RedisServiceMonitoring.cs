using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class RedisServiceMonitoring : ServiceMonitoringBase
    {

        public RedisServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            CuttingEdge.Conditions.Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.ConnectionString)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var connectionString = !string.IsNullOrEmpty(HealthCheck.ConnectionString) 
                ? HealthCheck.ConnectionString 
                : HealthCheck.EndpointOrHost;

            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.RedisBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new RedisHealthCheck(connectionString, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    /// <summary>
    /// Custom Redis health check implementation with connection reuse
    /// </summary>
    internal class RedisHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly TimeSpan _timeout;
        private static readonly object _lock = new object();
        private static IConnectionMultiplexer _sharedConnection;
        private static string _lastConnectionString;

        public RedisHealthCheck(string connectionString, TimeSpan timeout)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _timeout = timeout;
        }

        private async Task<IConnectionMultiplexer> GetOrCreateConnectionAsync()
        {
            lock (_lock)
            {
                if (_sharedConnection == null || !_sharedConnection.IsConnected || _lastConnectionString != _connectionString)
                {
                    _sharedConnection?.Dispose();

                    var options = ConfigurationOptions.Parse(_connectionString);
                    options.ConnectTimeout = (int)_timeout.TotalMilliseconds;
                    options.SyncTimeout = (int)_timeout.TotalMilliseconds;
                    options.AbortOnConnectFail = false;

                    _sharedConnection = ConnectionMultiplexer.Connect(options);
                    _lastConnectionString = _connectionString;
                }
                return _sharedConnection;
            }
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var connection = await GetOrCreateConnectionAsync();

                if (!connection.IsConnected)
                {
                    return HealthCheckResult.Unhealthy("Redis connection is not established");
                }

                var database = connection.GetDatabase();
                await database.PingAsync();

                return HealthCheckResult.Healthy("Redis connection is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Redis connection failed: {ex.Message}", ex);
            }
        }
    }
}
