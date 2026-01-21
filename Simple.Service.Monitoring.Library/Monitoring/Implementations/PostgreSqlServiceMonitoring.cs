using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class PostgreSqlServiceMonitoring : GenericSqlWithCustomResultValidationMonitoringBase
    {

        public PostgreSqlServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            var csbuilder = new DbConnectionStringBuilder();
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(this.HealthCheck.ConnectionString)
                .IsNotNull();
            Condition
                .Ensures(csbuilder.ConnectionString = this.HealthCheck.ConnectionString);
        }

        protected internal override void SetMonitoring()
        {
            var connectionString = !string.IsNullOrEmpty(this.HealthCheck.ConnectionString) 
                ? this.HealthCheck.ConnectionString 
                : this.HealthCheck.EndpointOrHost;

            var hasCustomQuery = !string.IsNullOrEmpty(this.HealthCheck.HealthCheckConditions?.SqlBehaviour?.Query);
            var query = hasCustomQuery ? this.HealthCheck.HealthCheckConditions.SqlBehaviour.Query : DEFAULTSQLQUERY;
            
            Func<object, HealthCheckResult> resultBuilder = null;
            if (hasCustomQuery)
            {
                resultBuilder = GetHealth;
            }

            HealthChecksBuilder.AddAsyncCheck(
                HealthCheck.Name,
                async () =>
                {
                    var healthCheck = new PostgreSqlHealthCheck(connectionString, query, resultBuilder);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    /// <summary>
    /// Custom PostgreSQL health check implementation
    /// </summary>
    internal class PostgreSqlHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _query;
        private readonly Func<object, HealthCheckResult> _resultBuilder;

        public PostgreSqlHealthCheck(string connectionString, string query, Func<object, HealthCheckResult> resultBuilder = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _query = query ?? "SELECT 1;";
            _resultBuilder = resultBuilder;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            NpgsqlConnection connection = null;
            try
            {
                connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = connection.CreateCommand();
                command.CommandText = _query;
                command.CommandTimeout = 30;

                var result = await command.ExecuteScalarAsync(cancellationToken);

                // If custom result builder provided, use it
                if (_resultBuilder != null)
                {
                    return _resultBuilder(result);
                }

                // Default: just check if we got a result
                return result != null
                    ? HealthCheckResult.Healthy($"PostgreSQL query executed successfully. Result: {result}")
                    : HealthCheckResult.Unhealthy("PostgreSQL query returned null");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"PostgreSQL connection failed: {ex.Message}", ex);
            }
            finally
            {
                // GUARANTEED disposal
                if (connection != null)
                {
                    try
                    {
                        await connection.CloseAsync();
                    }
                    finally
                    {
                        await connection.DisposeAsync();
                    }
                }
            }
        }
    }
}
