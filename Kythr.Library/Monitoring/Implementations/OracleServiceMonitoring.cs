using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oracle.ManagedDataAccess.Client;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class OracleServiceMonitoring : GenericSqlWithCustomResultValidationMonitoringBase
    {
        public OracleServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
            DEFAULTSQLQUERY = "SELECT 1 FROM DUAL";
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
                    var healthCheck = new OracleHealthCheck(connectionString, query, resultBuilder);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class OracleHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _query;
        private readonly Func<object, HealthCheckResult> _resultBuilder;

        public OracleHealthCheck(string connectionString, string query, Func<object, HealthCheckResult> resultBuilder = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _query = query ?? "SELECT 1 FROM DUAL";
            _resultBuilder = resultBuilder;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            OracleConnection connection = null;
            try
            {
                connection = new OracleConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = connection.CreateCommand();
                command.CommandText = _query;
                command.CommandTimeout = 30;

                var result = await command.ExecuteScalarAsync(cancellationToken);

                if (_resultBuilder != null)
                {
                    return _resultBuilder(result);
                }

                return HealthCheckResult.Healthy($"Oracle connection successful, query returned: {result}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"Oracle connection failed: {ex.Message}", ex);
            }
            finally
            {
                if (connection != null)
                {
                    await connection.CloseAsync();
                    connection.Dispose();
                }
            }
        }
    }
}
