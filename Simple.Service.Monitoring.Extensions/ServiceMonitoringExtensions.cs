using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Simple.Service.Monitoring.Library.Options;

namespace Simple.Service.Monitoring.Extensions
{
    public static class ServiceMonitoringExtensions
    {
        public static IServiceMonitoringBuilder UseServiceMonitoring(this IServiceCollection serviceCollection, 
            IConfiguration configuration)
        {

            //Refactor!!!!!!

            var monitoringsection = configuration.GetSection("Monitoring");

            serviceCollection.Configure<MonitorOptions>(monitoringsection);

            serviceCollection.AddSingleton<IStackMonitoring, StandardStackMonitoring>();

            serviceCollection.AddSingleton<IServiceMonitoringBuilder, ServiceMonitoringBuilder>();

            var healthChecksBuilder = serviceCollection.AddHealthChecks();

            serviceCollection.AddSingleton(provider => healthChecksBuilder);

            var sp = serviceCollection.BuildServiceProvider();

            var builder = sp.GetRequiredService<IServiceMonitoringBuilder>();

            return builder;
        }
    }
}
