using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class InterceptionMonitoring : ServiceMonitoringBase
    {
        public InterceptionMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
        }

        protected internal override void SetMonitoring()
        {
        }
    }
}
