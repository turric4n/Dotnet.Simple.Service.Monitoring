using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class RmqServiceMonitoring : ServiceMonitoringBase
    {

        public RmqServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition.Requires(HealthCheck.EndpointOrHost)
                .IsNotNull();

            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.IsWellFormedUriString(HealthCheck.EndpointOrHost, UriKind.Absolute))
                .IsTrue();
        }

        protected internal override void SetMonitoring()
        {
            var connectionFactory = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri(HealthCheck.EndpointOrHost)
            };

            HealthChecksBuilder.AddRabbitMQ(provider => connectionFactory.CreateConnectionAsync(), 
                HealthCheck.Name, 
                HealthStatus.Unhealthy, 
                GetTags());
        }
    }
}
