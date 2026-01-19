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
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.RedisBehaviour.TimeOutMs);

            HealthChecksBuilder.AddCheck(
                HealthCheck.Name,
                new RedisHealthCheck(HealthCheck.ConnectionString, timeout),
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                GetTags());
        }
    }

    /// <summary>
    /// Custom Redis health check implementation
    /// </summary>
    internal class RedisHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly TimeSpan _timeout;

        public RedisHealthCheck(string connectionString, TimeSpan timeout)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            IConnectionMultiplexer connection = null;
            try
            {
                var options = ConfigurationOptions.Parse(_connectionString);
                options.ConnectTimeout = (int)_timeout.TotalMilliseconds;
                options.SyncTimeout = (int)_timeout.TotalMilliseconds;
                options.AbortOnConnectFail = false;

                connection = await ConnectionMultiplexer.ConnectAsync(options);

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
            finally
            {
                if (connection != null)
                {
                    try
                    {
                        await connection.CloseAsync();
                    }
                    finally
                    {
                        connection.Dispose();
                    }
                }
            }
        }
    }
}
