using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.Service.Monitoring.Extensions
{
    public class ServiceMonitoringConfigurationService : IServiceMonitoringConfigurationService
    {
        private readonly IStackMonitoring _stackMonitoring;
        private MonitorOptions _options;
        //private readonly ILogger<ServiceMonitoringConfigurationService> _logger;

        public ServiceMonitoringConfigurationService(
            IStackMonitoring stackMonitoring,
            MonitorOptions options)
        {
            _stackMonitoring = stackMonitoring;
            _options = options;
        }

        public IServiceMonitoringConfigurationService WithAdditionalCheck(ServiceHealthCheck monitor)
        {
            //_logger.LogInformation($"Adding new health check monitor {monitor.Name} - {monitor.ServiceType}");
            _stackMonitoring.AddMonitoring(monitor);

            return this;
        }

        public IServiceMonitoringConfigurationService WithAdditionalPublishing(ServiceHealthCheck monitor)
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

        public IServiceMonitoringConfigurationService WithAdditionalPublisher(PublisherBase publisher)
        {
            _stackMonitoring.AddCustomPublisher(publisher);
            return this;
        }

        public IServiceMonitoringConfigurationService WithApplicationSettings()
        {
            var validOptions = _options?.HealthChecks != null;

            if (!validOptions) return this;

            foreach (var monitor in _options.HealthChecks)
            {
                monitor.Name = string.IsNullOrEmpty(monitor.Name)
                    ? _options.Settings?.UseGlobalServiceName
                    : monitor.Name;

                _stackMonitoring.AddMonitoring(monitor);

                this.WithAdditionalPublishing(monitor);
            }

            return this;
        }


        public IServiceMonitoringConfigurationService WithRuntimeSettings(MonitorOptions options)
        {
            this._options = options ?? throw new ArgumentNullException(nameof(options));
            var validOptions = _options?.HealthChecks != null;
            WithApplicationSettings();
            return this;
        }

        public IServiceMonitoringConfigurationService WithAdditionalPublisherObserver(IObserver<KeyValuePair<string, HealthReportEntry>> observer, bool useAlertingRules = true)
        {
            _stackMonitoring.GetPublishers()
                .ForEach(publisher =>
            {
                var observable = publisher;
                observable.Subscribe(observer);
            });

            return this;
        }
    }
}
