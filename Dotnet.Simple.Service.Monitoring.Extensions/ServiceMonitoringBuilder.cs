using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring;
using Dotnet.Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dotnet.Simple.Service.Monitoring.Extensions
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
        public IServiceMonitoringBuilder Add(ServiceMonitor monitor)
        {
            _stackMonitoring.AddMonitoring(monitor);
            return this;
        }

        public IServiceMonitoringBuilder UseSettings()
        {
            foreach (var monitor in _options.Value.Monitors)
            {
                _stackMonitoring.AddMonitoring(monitor);
            } 
            return this;
        }

        public IServiceMonitoringBuilder AddUI()
        {
            return this;
        }
    }
}
