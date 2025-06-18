using Microsoft.Extensions.Configuration;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.CallbackPublisher;
using Simple.Service.Monitoring.Library.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceMonitoringExtensions
    {
        public static IServiceMonitoringConfigurationService AddServiceMonitoring(this IServiceCollection serviceCollection, 
            IConfiguration configuration)
        {
            var monitoringSection = configuration.GetSection("Monitoring");

            serviceCollection
                .AddOptions<MonitorOptions>()
                .Bind(monitoringSection)
                .ValidateOnStart();

            var currentOptions = monitoringSection.Get<MonitorOptions>();

            var healthChecksBuilder = serviceCollection.AddHealthChecks();

            var stackMonitoring = new StandardStackMonitoring(healthChecksBuilder);

            var serviceMonitoringBuilder = new ServiceMonitoringConfigurationService(stackMonitoring, currentOptions);

            var callbackPublisher = new CallbackPublisher(healthChecksBuilder);

            stackMonitoring.AddCustomPublisher(callbackPublisher);

            serviceCollection.AddSingleton<IReportObservable>(callbackPublisher);

            serviceCollection.AddSingleton<IStackMonitoring>(stackMonitoring);

            return serviceMonitoringBuilder;
        }
    }
}
