using CuttingEdge.Conditions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class MsSqlServiceMonitoring : GenericSqlWithCustomResultValidationMonitoringBase
    {

        public MsSqlServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .Requires(this.HealthCheck.HealthCheckConditions)
                .IsNotNull();

            var hasEndpointOrHost = !string.IsNullOrEmpty(this.HealthCheck.EndpointOrHost);
            var hasConnectionString = !string.IsNullOrEmpty(this.HealthCheck.ConnectionString);

            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(hasEndpointOrHost || hasConnectionString)
                .IsTrue("Either EndpointOrHost or ConnectionString must be provided");

            var csbuilder = new DbConnectionStringBuilder();
            var connectionString = hasConnectionString ? this.HealthCheck.ConnectionString : this.HealthCheck.EndpointOrHost;
            
            Condition
                .Ensures(csbuilder.ConnectionString = connectionString);
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
                    var healthCheck = new SqlServerHealthCheck(connectionString, query, resultBuilder);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    /// <summary>
    /// Custom SQL Server health check implementation
    /// </summary>
    internal class SqlServerHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _query;
        private readonly Func<object, HealthCheckResult> _resultBuilder;

        public SqlServerHealthCheck(string connectionString, string query, Func<object, HealthCheckResult> resultBuilder = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _query = query ?? "SELECT 1;";
            _resultBuilder = resultBuilder;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = connection.CreateCommand();
                command.CommandText = _query;
                command.CommandTimeout = 30;

                var result = await command.ExecuteScalarAsync(cancellationToken);

                // If we have a custom result builder, use it
                if (_resultBuilder != null)
                {
                    return _resultBuilder(result);
                }

                // Default behavior: just check if query executed successfully
                return HealthCheckResult.Healthy($"SQL Server connection successful, query returned: {result}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"SQL Server connection failed: {ex.Message}", ex);
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
