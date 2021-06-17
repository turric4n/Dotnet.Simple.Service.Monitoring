using System;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class ServiceMonitoringBase : IServiceMonitoring
    {
        protected readonly IHealthChecksBuilder HealthChecksBuilder;
        protected readonly ServiceHealthCheck HealthCheck;
        protected readonly Guid MonitorId;

        public readonly string Name;
        

        protected ServiceMonitoringBase(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
        {
            HealthChecksBuilder = healthChecksBuilder;
            HealthCheck = healthCheck;
            MonitorId = new Guid();
            Name = healthCheck.Name;
        }

        protected internal abstract void Validate();

        public void SetUp()
        {
            Validate();
            SetMonitoring();
        }

        protected internal abstract void SetMonitoring();
    }
}
