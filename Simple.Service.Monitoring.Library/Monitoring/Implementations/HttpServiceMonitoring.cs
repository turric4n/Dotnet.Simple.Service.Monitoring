using System;
using System.Collections.Generic;
using System.Linq;
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
            foreach (var host in this._healthCheck.EndpointOrHost.Split(','))
            {
                Condition.Requires(this._healthCheck.HealthCheckConditions)
                    .IsNotNull();
                Condition.Requires(this._healthCheck.HealthCheckConditions.HttpBehaviour)
                    .IsNotNull();
                Condition.Requires(this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedCode)
                    .IsGreaterThan(0);
                Condition
                    .WithExceptionOnFailure<MalformedUriException>()
                    .Requires(Uri.IsWellFormedUriString(host, UriKind.Absolute))
                    .IsTrue();
            }
        }

        protected internal override void SetMonitoring()
        {
            this._healthChecksBuilder.AddUrlGroup((options) =>
            {
                foreach (var endpoint in this._healthCheck.EndpointOrHost.Split(','))
                {
                    var uri = new Uri(endpoint);
                    options.AddUri(uri);
                    options.ExpectHttpCode(this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedCode);
                    switch (this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpVerb)
                    {
                        case HttpVerb.Get:
                            options.UseGet();
                            break;
                        case HttpVerb.Post:
                            options.UsePost();
                            break;
                        case HttpVerb.Put:
                            break;
                        case HttpVerb.Delete:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedResponseTimeMs > 0)
                    {
                        //options.UseTimeout(TimeSpan.FromMilliseconds(this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedResponseTimeMs));
                    }
                }

            }, _healthCheck.Name);
        }

    }
}
