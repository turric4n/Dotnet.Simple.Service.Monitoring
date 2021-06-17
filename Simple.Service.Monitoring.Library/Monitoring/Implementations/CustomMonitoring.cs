using System;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client.Events;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class CustomMonitoring : ServiceMonitoringBase
    {

        public CustomMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
        }

        internal Func<Task<HealthCheckResult>> CustomCheck;

        public void AddCustomCheck(Func<Task<HealthCheckResult>> check)
        {
            CustomCheck = check;
        }

        protected internal override void SetMonitoring()
        {
            this.HealthChecksBuilder.AddAsyncCheck(Name, () =>
            {
                return CustomCheck != null ? CustomCheck() : Task.FromResult(HealthCheckResult.Healthy());
            });
        }
    }
}
