using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System.Data.Common;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class HangfireServiceMonitoring : ServiceMonitoringBase
    {

        public HangfireServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
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
            HealthChecksBuilder.AddHangfire(options =>
            {
                var minimumAvailableServers =
                    this.HealthCheck.HealthCheckConditions.HangfireBehaviour?.MinimumAvailableServers;

                var maximumJobsFailed =
                    this.HealthCheck.HealthCheckConditions.HangfireBehaviour?.MinimumAvailableServers;

                options.MinimumAvailableServers = minimumAvailableServers;

                options.MaximumJobsFailed = maximumJobsFailed;
            }, this.HealthCheck.Name);
        }

    }
}
