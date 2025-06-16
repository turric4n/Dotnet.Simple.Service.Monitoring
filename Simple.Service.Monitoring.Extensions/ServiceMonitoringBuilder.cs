using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Options;
using System;
using System.Linq;

namespace Simple.Service.Monitoring.Extensions
{
    public class ServiceMonitoringBuilder : IServiceMonitoringBuilder
    {
        private readonly IStackMonitoring _stackMonitoring;
        private readonly MonitorOptions _options;
        //private readonly ILogger<ServiceMonitoringBuilder> _logger;

        public ServiceMonitoringBuilder(
            IStackMonitoring stackMonitoring,
            MonitorOptions options)
        {
            _stackMonitoring = stackMonitoring;
            _options = options;
        }

        public IServiceMonitoringBuilder Add(ServiceHealthCheck monitor)
        {
            //_logger.LogInformation($"Adding new health check monitor {monitor.Name} - {monitor.ServiceType}");
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
                            transport = _options.EmailTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.CustomApi:
                            transport = _options.CustomNotificationTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.Telegram:
                            transport = _options.TelegramTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.Influx:
                            transport = _options.InfluxDbTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        case AlertTransportMethod.Slack:
                            transport = _options.SlackTransportSettings
                                .FirstOrDefault(x => x.Name == ab.TransportName);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    if (transport == null)
                    {
                        //_logger.LogError($"Error adding alert publisher transport settings {ab.TransportName}");
                        return;
                    }

                    //_logger.LogInformation($"Adding new health check publisher {transport.Name} {transport.GetType().Name}");
                    _stackMonitoring.AddPublishing(transport, monitor);
                });
            }

            return this;
        }

        public IServiceMonitoringBuilder UseSettings()
        {
            var validOptions = _options?.HealthChecks != null;

            if (!validOptions) return this;

            foreach (var monitor in _options.HealthChecks)
            {
                monitor.Name = string.IsNullOrEmpty(monitor.Name)
                    ? _options.Settings?.UseGlobalServiceName
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
    }
}
