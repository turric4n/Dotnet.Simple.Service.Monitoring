using System;
using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System.Data.Common;
using HealthChecks.MySql;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using HealthChecks.SqlServer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

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

            if (this.HealthCheck.HealthCheckConditions.SqlBehaviour != null)
            {
                sqlOptions.CommandText = HealthCheck.HealthCheckConditions.SqlBehaviour?.Query ?? this.DEFAULTSQLQUERY;
                sqlOptions.ConnectionString = this.HealthCheck.ConnectionString;
                sqlOptions.HealthCheckResultBuilder = GetHealth;

                HealthChecksBuilder.AddMySql(sqlOptions, HealthCheck.Name);
            }
            else
            {
                HealthChecksBuilder.AddMySql(this.HealthCheck.ConnectionString, HealthCheck.Name);
            }
        }
    }
}
