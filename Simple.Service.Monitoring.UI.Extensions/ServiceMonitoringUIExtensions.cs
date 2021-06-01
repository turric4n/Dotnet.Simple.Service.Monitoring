using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceMonitoringUIExtensions
    {
        public static IServiceMonitoringBuilder AddUI(this IServiceMonitoringBuilder monitoringBuilder, IServiceCollection services)
        {
            return monitoringBuilder;
        }
    }
}
