using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Simple.Service.Monitoring.Library.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceMonitoringExtensions
    {
        public static IServiceCollection AddServiceMonitoring(this IServiceCollection serviceCollection, 
            IConfiguration configuration)
        {
            var monitoringsection = configuration.GetSection("Monitoring");

            serviceCollection.Configure<MonitorOptions>(monitoringsection);

            serviceCollection.AddSingleton<IServiceMonitoringBuilder, ServiceMonitoringBuilder>();

            serviceCollection.AddSingleton<IStackMonitoring, StandardStackMonitoring>();

            var healthChecksBuilder = serviceCollection.AddHealthChecks();

            serviceCollection.AddSingleton(provider => healthChecksBuilder);

            return serviceCollection;
        }

        public static IServiceMonitoringBuilder UseServiceMonitoring(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.ApplicationServices.GetService<IServiceMonitoringBuilder>();
        }
    }
}
