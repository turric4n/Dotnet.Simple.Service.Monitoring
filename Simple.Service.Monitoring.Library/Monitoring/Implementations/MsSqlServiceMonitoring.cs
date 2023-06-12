using CuttingEdge.Conditions;
using HealthChecks.MySql;
using HealthChecks.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System.Data.Common;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class MsSqlServiceMonitoring : GenericSqlWithCustomResultValidationMonitoringBase
    {

        public MsSqlServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            var csbuilder = new DbConnectionStringBuilder();
            Condition
                .Requires(this.HealthCheck.HealthCheckConditions)
                .IsNotNull();
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(this.HealthCheck.ConnectionString)
                .IsNotNull();
            Condition
                .Ensures(csbuilder.ConnectionString = this.HealthCheck.ConnectionString);
        }

        protected internal override void SetMonitoring()
        {
            var sqlOptions = new SqlServerHealthCheckOptions();

            if (!string.IsNullOrEmpty(this.HealthCheck.HealthCheckConditions.SqlBehaviour.Query))
            {
                sqlOptions.ConnectionString = this.HealthCheck.ConnectionString;
                sqlOptions.HealthCheckResultBuilder = GetHealth;

                HealthChecksBuilder.AddSqlServer(sqlOptions, HealthCheck.Name);
            }
            else
            {
                HealthChecksBuilder.AddSqlServer(this.HealthCheck.ConnectionString, HealthCheck.Name);
            }
        }
    }
}
