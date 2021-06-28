using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class RmqServiceMonitoring : ServiceMonitoringBase
    {

        public RmqServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            //var csbuilder = new DbConnectionStringBuilder();
            //csbuilder.ConnectionString = HealthCheck.ConnectionString;

            Condition.Requires(this.HealthCheck.ConnectionString)
                .IsNotNull();
            //Condition
            //    .Ensures(csbuilder.ConnectionString = this.HealthCheck.ConnectionString);
        }

        protected internal override void SetMonitoring()
        {
            HealthChecksBuilder.AddRabbitMQ(this.HealthCheck.ConnectionString, null, this.HealthCheck.Name, HealthStatus.Unhealthy, null, TimeSpan.FromSeconds(5));
        }
    }
}
