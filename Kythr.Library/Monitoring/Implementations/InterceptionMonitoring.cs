using Microsoft.Extensions.DependencyInjection;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;

namespace Kythr.Library.Monitoring.Implementations
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
