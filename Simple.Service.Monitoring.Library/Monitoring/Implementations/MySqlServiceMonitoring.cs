using CuttingEdge.Conditions;
using HealthChecks.MySql;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System.Data.Common;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class MySqlServiceMonitoring : GenericSqlWithCustomResultValidationMonitoringBase
    {

        public MySqlServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
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
            var sqlOptions = new MySqlHealthCheckOptions();

            if (!string.IsNullOrEmpty(this.HealthCheck.HealthCheckConditions.SqlBehaviour.Query))
            {
                sqlOptions.ConnectionString = this.HealthCheck.ConnectionString;
                sqlOptions.HealthCheckResultBuilder = GetHealth;

                HealthChecksBuilder.AddMySql(sqlOptions, HealthCheck.Name);
            }
            else
            {
                HealthChecksBuilder.AddMySql(this.HealthCheck.ConnectionString, 
                    DEFAULTSQLQUERY, null, HealthCheck.Name, null, GetTags());
            }
        }
    }
}
