using System;
using System.Runtime.CompilerServices;
using Dotnet.Simple.Service.Monitoring.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;

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
