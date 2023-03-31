using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;

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
            var connectionFactory = new RabbitMQ.Client.ConnectionFactory();
            connectionFactory.Uri = new Uri(HealthCheck.EndpointOrHost);

            HealthChecksBuilder.AddRabbitMQ(provider =>
            {
                return connectionFactory;
            }, HealthCheck.Name, HealthStatus.Unhealthy);
        }
    }
}
