using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Simple.Service.Monitoring.Library.ConfigurationLoader;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring;
using Dotnet.Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet.Simple.Service.Monitoring.Extensions
{
    public static class ServiceMonitoringExtensions
    {
        public static IServiceMonitoringBuilder UseServiceMonitoring(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var monitoringsection = configuration.GetSection("Monitoring");

            serviceCollection.Configure<MonitorOptions>(monitoringsection);

            serviceCollection.AddSingleton<IStackMonitoring, StandardStackMonitoring>();

            serviceCollection.AddSingleton<IServiceMonitoringBuilder, ServiceMonitoringBuilder>();

            var sp = serviceCollection.BuildServiceProvider();

            var monitoring = sp.GetRequiredService<IStackMonitoring>();

            var builder = sp.GetRequiredService<IServiceMonitoringBuilder>();

            return builder;
        }
    }
}
