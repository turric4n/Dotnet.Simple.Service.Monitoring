using System; 
using System.Linq;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Simple.Service.Monitoring.Extensions
{
    public class ServiceMonitoringBuilder : IServiceMonitoringBuilder
    {
        private readonly IStackMonitoring _stackMonitoring;
        private readonly IOptions<MonitorOptions> _options;
        private readonly ILogger<ServiceMonitoringBuilder> _logger;

        public ServiceMonitoringBuilder(IStackMonitoring stackMonitoring, IOptions<MonitorOptions> options, ILogger<ServiceMonitoringBuilder> logger)
        {
            _stackMonitoring = stackMonitoring;
            _options = options;
            _logger = logger;
        }

        public IServiceMonitoringBuilder Add(ServiceHealthCheck monitor)
        {
            _logger.LogInformation($"Adding new health check monitor {monitor.Name} - {monitor.ServiceType}");
            _stackMonitoring.AddMonitoring(monitor);

            return this;
        }

        public IServiceMonitoringBuilder AddPublishing(ServiceHealthCheck monitor)
        {
            if (monitor.Alert)
            {
                monitor.AlertBehaviour?.ForEach(ab =>
                {
                    AlertTransportSettings transport;
                    switch (ab.TransportMethod)
                    {
                        case AlertTransportMethod.Email:
                            transport = _options.Value.EmailTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.CustomApi:
                            transport = _options.Value.CustomNotificationTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.Telegram:
                            transport = _options.Value.TelegramTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.Influx:
                            transport = _options.Value.InfluxDbTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.Slack:
                            transport = _options.Value.SlackTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (transport == null) return;

                    _logger.LogInformation($"Adding new health check publisher {transport.Name} {transport.GetType().Name}");
                    _stackMonitoring.AddPublishing(transport, monitor);
                });
            }

            return this;
        }

        public IServiceMonitoringBuilder UseSettings()
        {
            var validOptions = _options?.Value.HealthChecks != null;

            if (!validOptions) return this;

            foreach (var monitor in _options.Value.HealthChecks)
            {
                monitor.Name = string.IsNullOrEmpty(monitor.Name)
                    ? _options.Value.Settings?.UseGlobalServiceName
                    : monitor.Name;

                this.Add(monitor);

                this.AddPublishing(monitor);
            }

            return this;
        }

        public IServiceMonitoringBuilder AddPublisherObserver(IObserver<HealthReport> observer)
        {
            _stackMonitoring.GetPublishers()
                .ForEach(publisher =>
            {
                var observable = (IObservable<HealthReport>)publisher;
                observable.Subscribe(observer);
            });

            return this;
        }

        public IStackMonitoring Build()
        {
            return _stackMonitoring;
        }
    }
}
