using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring
{
    public class HttpServiceMonitoring : ServiceMonitoringBase
    {

        public HttpServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }
        public override void Launch()
        {
            this._healthChecksBuilder.add
        }
    }
}
