using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Simple.Service.Monitoring.Extensions
{
    public class ServiceMonitoringBuilder : IServiceMonitoringBuilder
    {
        private readonly IStackMonitoring _stackMonitoring;
        private readonly IOptions<MonitorOptions> _options;

        public ServiceMonitoringBuilder(IStackMonitoring stackMonitoring, IOptions<MonitorOptions> options)
        {
            _stackMonitoring = stackMonitoring;
            _options = options;
        }

        public IServiceMonitoringBuilder Add(ServiceHealthCheck monitor)
        {
            _stackMonitoring.AddMonitoring(monitor);

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

                _stackMonitoring.AddMonitoring(monitor);

                if (monitor.Alert)
                {
                    monitor.AlertBehaviour?.ForEach(ab =>
                    {
                        AlertTransportSettings transport = null;
                        switch (ab.TransportMethod)
                        {
                            case AlertTransportMethod.Email:
                                transport = _options.Value.EmailTransportSettings
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
                        if (transport != null)
                        {
                            _stackMonitoring.AddPublishing(transport, monitor);
                        }
                    });
                }
            }

            return this;
        }

        public IServiceMonitoringBuilder AddPublisherObserver(IObserver<HealthReport> observer)
        {
            _stackMonitoring.GetPublishers().ForEach(x =>
            {
                var observable = (IObservable<HealthReport>) x;
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
