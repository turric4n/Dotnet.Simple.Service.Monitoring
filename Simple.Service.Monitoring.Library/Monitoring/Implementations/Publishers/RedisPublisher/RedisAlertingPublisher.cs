using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Condition = CuttingEdge.Conditions.Condition;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.RedisPublisher
{
    public class RedisAlertingPublisher : PublisherBase, IDisposable
    {
        private readonly RedisTransportSettings _redisTransportSettings;
        private static readonly object _connectionLock = new object();
        private ConnectionMultiplexer _connection;
        private bool _disposed = false;

        public RedisAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _redisTransportSettings = (RedisTransportSettings)alertTransportSettings;
        }

        private ConnectionMultiplexer GetConnection()
        {
            if (_connection != null && _connection.IsConnected)
                return _connection;

            lock (_connectionLock)
            {
                if (_connection != null && _connection.IsConnected)
                    return _connection;

                // Dispose old connection if it exists
                if (_connection != null)
                {
                    _connection.Dispose();
                }

                // Create connection configuration
                var options = new ConfigurationOptions
                {
                    EndPoints = { { _redisTransportSettings.Host, _redisTransportSettings.Port } },
                    AbortOnConnectFail = false, // Don't abort if connection fails initially
                    ConnectRetry = 3, // Retry 3 times before giving up
                    ConnectTimeout = 5000, // Connection timeout of 5 seconds
                    SyncTimeout = 5000, // Sync operations timeout of 5 seconds
                    ReconnectRetryPolicy = new LinearRetry(1000), // Retry every second
                };

                _connection = ConnectionMultiplexer.Connect(options);
                _connection.ConnectionFailed += (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Redis connection failed: {args.Exception.Message}");
                };

                _connection.ConnectionRestored += (sender, args) =>
                {
                    System.Diagnostics.Debug.WriteLine("Redis connection restored");
                };

                return _connection;
            }
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            try
            {
                var ownedEntry = this.GetOwnedEntry(report);
                var interceptedEntries = this.GetInterceptedEntries(report);

                var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

                if (ownedAlerting)
                {
                    await PublishToRedisAsync(ownedEntry, cancellationToken);
                }

                foreach (var interceptedEntry in interceptedEntries)
                {
                    if (this.IsOkToAlert(interceptedEntry, true))
                    {
                        await PublishToRedisAsync(interceptedEntry, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception, but don't throw to avoid breaking health checks
                System.Diagnostics.Debug.WriteLine($"Failed to publish health report to Redis: {ex.Message}");
            }
        }

        private async Task PublishToRedisAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                // Get the connection from the pool (or create a new one)
                var connection = GetConnection();
                var database = connection.GetDatabase(_redisTransportSettings.DatabaseNumber);

                // Create a channel name for publishing health reports
                var channelName = RedisChannel.Literal($"health-check:{entry.Key}");

                // Serialize the health check entry to JSON
                var serializedReport = System.Text.Json.JsonSerializer.Serialize(new
                {
                    TimeStamp = DateTime.Now,
                    ServiceName = entry.Key,
                    ServiceType = _healthCheck.ServiceType.ToString(),
                    Status = entry.Value.Status.ToString(),
                    Duration = entry.Value.Duration.TotalMilliseconds,
                    Description = entry.Value.Description,
                    Error = entry.Value.Exception?.Message,
                    Data = entry.Value.Data
                });

                // Publish the health report to Redis
                await database.PublishAsync(channelName, serializedReport);
                
                // Also store the latest state in a key with the same name
                await database.StringSetAsync(
                    $"health-check-latest:{entry.Key}", 
                    serializedReport,
                    expiry: TimeSpan.FromDays(1) // Expire after 1 day to prevent stale data
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to publish entry {entry.Key} to Redis: {ex.Message}");
            }
        }
        
        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<RedisValidationError>()
                .Requires(_redisTransportSettings.Host)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
            }

            _disposed = true;
        }
        
        ~RedisAlertingPublisher()
        {
            Dispose(false);
        }
    }
}
