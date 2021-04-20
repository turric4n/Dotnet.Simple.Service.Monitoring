using System;
using System.Collections.Generic;
using System.Text;
using CuttingEdge.Conditions;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring
{
    public class HttpServiceMonitoring : ServiceMonitoringBase
    {

        public HttpServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected override void Validate()
        {
            foreach (var host in this._healthCheck.EndpointOrHost.Split(','))
            {
                Condition.Requires(this._healthCheck.HealthCheckConditions)
                    .IsNotNull();
                Condition.Requires(this._healthCheck.HealthCheckConditions.HttpBehaviour)
                    .IsNotNull();
                Condition.Requires(this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpExpectedCode)
                    .IsGreaterThan(0);
                Condition.Requires(Uri.IsWellFormedUriString(host, UriKind.Absolute))
                    .IsTrue();
            }
        }

        public override void SetUp()
        {
            Validate();
            var urilist = new List<Uri>();
            this._healthCheck.EndpointOrHost.Split(',')
                .ToList()
                .ForEach(x =>
                {
                    var uri = new Uri(x);
                    this._healthChecksBuilder.AddUrlGroup((options) =>
                    {
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

                        if (this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpResponseTimesSeconds > 0)
                        {
                            options.UseTimeout(TimeSpan.FromSeconds(this._healthCheck.HealthCheckConditions.HttpBehaviour.HttpResponseTimesSeconds));
                        }
                    });
                });

            //if (this._healthCheck.AlertBehaviour.)
            //this._healthChecksBuilder.Services.
        }

    }
}
