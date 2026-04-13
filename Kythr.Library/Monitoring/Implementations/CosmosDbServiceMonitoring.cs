using CuttingEdge.Conditions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class CosmosDbServiceMonitoring : ServiceMonitoringBase
    {
        public CosmosDbServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
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
            var databaseId = HealthCheck.HealthCheckConditions.CosmosDbBehaviour.DatabaseId;
            var containerId = HealthCheck.HealthCheckConditions.CosmosDbBehaviour.ContainerId;
            var timeout = TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.CosmosDbBehaviour.TimeOutMs);

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new CosmosDbHealthCheck(connectionString, databaseId, containerId, timeout);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class CosmosDbHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _databaseId;
        private readonly string _containerId;
        private readonly TimeSpan _timeout;

        public CosmosDbHealthCheck(string connectionString, string databaseId, string containerId, TimeSpan timeout)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _databaseId = databaseId;
            _containerId = containerId;
            _timeout = timeout;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            CosmosClient client = null;
            try
            {
                var options = new CosmosClientOptions
                {
                    RequestTimeout = _timeout,
                    ConnectionMode = ConnectionMode.Gateway
                };

                client = new CosmosClient(_connectionString, options);
                var accountProperties = await client.ReadAccountAsync();

                if (!string.IsNullOrEmpty(_databaseId))
                {
                    var database = client.GetDatabase(_databaseId);
                    await database.ReadAsync(cancellationToken: cancellationToken);

                    if (!string.IsNullOrEmpty(_containerId))
                    {
                        var container = database.GetContainer(_containerId);
                        await container.ReadContainerAsync(cancellationToken: cancellationToken);
                    }
                }

                return HealthCheckResult.Healthy($"CosmosDB connection healthy. Account: {accountProperties.Id}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"CosmosDB connection failed: {ex.Message}", ex);
            }
            finally
            {
                client?.Dispose();
            }
        }
    }
}
