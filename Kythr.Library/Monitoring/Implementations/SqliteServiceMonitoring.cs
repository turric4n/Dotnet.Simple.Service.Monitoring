using CuttingEdge.Conditions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Exceptions;
using System;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Kythr.Library.Monitoring.Implementations
{
    public class SqliteServiceMonitoring : GenericSqlWithCustomResultValidationMonitoringBase
    {
        public SqliteServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
            : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .Requires(this.HealthCheck.HealthCheckConditions)
                .IsNotNull();

            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.ConnectionString)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var connectionString = HealthCheck.ConnectionString;
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
                    var healthCheck = new SqliteHealthCheck(connectionString, query, resultBuilder);
                    return await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);
                },
                GetTags());
        }
    }

    internal class SqliteHealthCheck : IHealthCheck
    {
        private readonly string _connectionString;
        private readonly string _query;
        private readonly Func<object, HealthCheckResult> _resultBuilder;

        public SqliteHealthCheck(string connectionString, string query, Func<object, HealthCheckResult> resultBuilder = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _query = query ?? "SELECT 1;";
            _resultBuilder = resultBuilder;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            SqliteConnection connection = null;
            try
            {
                connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                using var command = connection.CreateCommand();
                command.CommandText = _query;

                var result = await command.ExecuteScalarAsync(cancellationToken);

                if (_resultBuilder != null)
                {
                    return _resultBuilder(result);
                }

                return HealthCheckResult.Healthy($"SQLite connection successful, query returned: {result}");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"SQLite connection failed: {ex.Message}", ex);
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
