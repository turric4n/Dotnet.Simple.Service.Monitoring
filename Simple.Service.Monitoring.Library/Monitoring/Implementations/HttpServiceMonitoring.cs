using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CuttingEdge.Conditions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class HttpServiceMonitoring : ServiceMonitoringBase
    {

        public HttpServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            foreach (var host in this.HealthCheck.EndpointOrHost.Split(','))
            {
                Condition.Requires(this.HealthCheck.HealthCheckConditions)
                    .IsNotNull();
                Condition.Requires(this.HealthCheck.HealthCheckConditions.HttpBehaviour)
                    .IsNotNull();
                Condition.Requires(this.HealthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedCode)
                    .IsGreaterThan(0);
                Condition
                    .WithExceptionOnFailure<MalformedUriException>()
                    .Requires(Uri.IsWellFormedUriString(host, UriKind.Absolute))
                    .IsTrue();
            }
        }

        protected internal override void SetMonitoring()
        {
            this.HealthChecksBuilder.AddUrlGroup((options) =>
            {
                foreach (var endpoint in this.HealthCheck.EndpointOrHost.Split(','))
                {
                    var uri = new Uri(endpoint);
                    options.AddUri(uri);
                    options.ExpectHttpCode(this.HealthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedCode);
                    switch (this.HealthCheck.HealthCheckConditions.HttpBehaviour.HttpVerb)
                    {
                        case HttpVerb.Get:
                            options.UseGet();
                            break;
                        case HttpVerb.Post:
                            options.UsePost();
                            break;
                        case HttpVerb.Put:
                            options.UseHttpMethod(HttpMethod.Put);
                            break;
                        case HttpVerb.Delete:
                            options.UseHttpMethod(HttpMethod.Delete);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

            }, HealthCheck.Name);
        }

    }
}
