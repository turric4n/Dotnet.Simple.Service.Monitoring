using System;
using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using HealthChecks.SqlServer;

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
            var sqlOptions = new SqlServerHealthCheckOptions()
            {
                CommandText = this.HealthCheck.HealthCheckConditions.SqlBehaviour.Query,
                ConnectionString = this.HealthCheck.ConnectionString,
                HealthCheckResultBuilder = base.GetHealth
            };

            var sqlBehaviourQuery = this.HealthCheck.HealthCheckConditions.SqlBehaviour?.Query;
            if (sqlBehaviourQuery != null)
                HealthChecksBuilder.AddSqlServer(sqlOptions, HealthCheck.Name,
                    HealthStatus.Unhealthy, null, null);
        }
    }
}
