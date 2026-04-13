using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class MongoDbServiceMonitoring : ServiceMonitoringBase
    {
        public MongoDbServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.ConnectionString)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var connectionString = HealthCheck.ConnectionString;
            var databaseName = HealthCheck.HealthCheckConditions.MongoDbBehaviour.DatabaseName;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.MongoDbBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new MongoDbHealthCheck(connectionString, databaseName, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class MongoDbHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly TimeSpan _timeout;

        public MongoDbHealthCheck(string connectionString, string databaseName, TimeSpan timeout)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseName = databaseName;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString(_connectionString);
                settings.ConnectTimeout = _timeout;
                settings.ServerSelectionTimeout = _timeout;

                var client = new MongoClient(settings);
                var database = client.GetDatabase(_databaseName ?? "admin");

                var pingCommand = new BsonDocument("ping", 1);
                await database.RunCommandAsync<BsonDocument>(pingCommand, cancellationToken: cancellationToken);

                return HealthCheckResult.Healthy("MongoDB connection is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"MongoDB connection failed: {ex.Message}", ex);
            }
        }
    }
}
