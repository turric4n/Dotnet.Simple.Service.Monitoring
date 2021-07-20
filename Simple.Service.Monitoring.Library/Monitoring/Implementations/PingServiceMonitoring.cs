using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class PingServiceMonitoring : ServiceMonitoringBase
    {

        public PingServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.CheckHostName(HealthCheck.EndpointOrHost))
                .IsNotEqualTo(UriHostNameType.Unknown);
        }

        protected internal override void SetMonitoring()
        {
            this.HealthChecksBuilder.AddPingHealthCheck((options) =>
            {
                foreach (var endpoint in this.HealthCheck.EndpointOrHost.Split(','))
                {
                    options.AddHost(endpoint, 1000);
                }
            }, HealthCheck.Name);
        }

    }
}
