using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class RedisServiceMonitoring : ServiceMonitoringBase
    {

        public RedisServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<InvalidConnectionStringException>()
                .Requires(HealthCheck.ConnectionString)
                .IsNotNullOrEmpty();
        }

        protected internal override void SetMonitoring()
        {
            var connection = HealthCheck.ConnectionString;

            this.HealthChecksBuilder.AddRedis(connection, HealthCheck.Name, HealthStatus.Unhealthy,
                GetTags(), TimeSpan.FromMilliseconds(HealthCheck.HealthCheckConditions.RedisBehaviour.TimeOutMs));
        }
    }
}
