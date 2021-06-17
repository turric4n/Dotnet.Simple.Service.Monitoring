using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class MsSqlServiceMonitoring : ServiceMonitoringBase
    {

        public MsSqlServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            var csbuilder = new DbConnectionStringBuilder();
            Condition.Requires(this.HealthCheck.HealthCheckConditions)
                .IsNotNull();
            Condition.Requires(this.HealthCheck.ConnectionString)
                .IsNotNull();
            Condition
                .Ensures(csbuilder.ConnectionString = this.HealthCheck.ConnectionString);
        }

        protected internal override void SetMonitoring()
        {
            HealthChecksBuilder.AddSqlServer(this.HealthCheck.ConnectionString);
        }

    }
}
