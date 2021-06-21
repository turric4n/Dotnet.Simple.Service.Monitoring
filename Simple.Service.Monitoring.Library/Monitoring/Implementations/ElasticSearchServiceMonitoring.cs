﻿using System;
using System.Collections.Generic;
using System.Linq;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class ElasticSearchServiceMonitoring : ServiceMonitoringBase
    {

        public ElasticSearchServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            Condition.Requires(this.HealthCheck.HealthCheckConditions)
                .IsNotNull();
            Condition
                .WithExceptionOnFailure<MalformedUriException>()
                .Requires(Uri.IsWellFormedUriString(this.HealthCheck.EndpointOrHost, UriKind.Absolute))
                .IsTrue();
    }

        protected internal override void SetMonitoring()
        {
            this.HealthChecksBuilder.AddElasticsearch((options) =>
            {
                var uri = new Uri(this.HealthCheck.EndpointOrHost);
                options.UseServer(uri.ToString());
            }, HealthCheck.Name);
        }

    }
}