﻿using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class ElasticSearchServiceMonitoring : ServiceMonitoringBase
    {

        public ElasticSearchServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.IsWellFormedUriString(this.HealthCheck.EndpointOrHost, UriKind.Absolute))
                .IsTrue();
    }

        protected internal override void SetMonitoring()
        {
            this.HealthChecksBuilder.AddElasticsearch(HealthCheck.EndpointOrHost, HealthCheck.Name, HealthStatus.Unhealthy, this.GetTags(), TimeSpan.FromSeconds(5));
        }

    }
}
