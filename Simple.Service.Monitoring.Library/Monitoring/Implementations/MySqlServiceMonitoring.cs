using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System.Data.Common;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class MySqlServiceMonitoring : ServiceMonitoringBase
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
            HealthChecksBuilder.AddMySql(this.HealthCheck.ConnectionString);
        }
    }
}
