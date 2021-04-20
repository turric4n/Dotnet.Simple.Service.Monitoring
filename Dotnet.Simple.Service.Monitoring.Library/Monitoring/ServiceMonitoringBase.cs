using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring
{
    public abstract class ServiceMonitoringBase : IServiceMonitoring
    {
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;

        protected ServiceMonitoringBase(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _healthCheck = healthCheck;
        }
        public abstract void Launch();
    }
}
