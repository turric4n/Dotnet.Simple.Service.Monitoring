using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Dotnet.Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Dotnet.Simple.Service.Monitoring.Extensions
{
    public class ServiceMonitoringBuilder : IServiceMonitoringBuilder
    {
        private readonly IStackMonitoring _stackMonitoring;
        private readonly IOptions<MonitorOptions> _options;

        public ServiceMonitoringBuilder(IStackMonitoring stackMonitoring, IOptions<MonitorOptions> options = null)
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
            if (_options.Value == null) return this;

            foreach (var monitor in _options.Value.HealthChecks)
            {
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

        public IServiceMonitoringBuilder AddObserver(IObserver<HealthReport> observer)
        {
            _stackMonitoring.GetPublishers().ForEach(x =>
            {
                var observable = (IObservable<HealthReport>) x;
                observable.Subscribe(observer);
            });

            return this;
        }
    }
}
