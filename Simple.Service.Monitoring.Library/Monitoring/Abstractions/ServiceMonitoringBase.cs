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
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;
        protected readonly Guid _monitorId;
        protected readonly string _name;
        
        protected ServiceMonitoringBase(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _healthCheck = healthCheck;
            _monitorId = new Guid();
            _name = healthCheck.Name;
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
