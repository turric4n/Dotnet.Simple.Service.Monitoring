using System.Collections.Generic;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public interface IStackMonitoring
    {
        IStackMonitoring AddMonitoring(ServiceHealthCheck monitor);
        IStackMonitoring AddPublishing(AlertTransportSettings alertTransportSettings, ServiceHealthCheck monitor);
        CustomMonitoring GetCustomMonitor(string name);
        List<ServiceMonitoringBase> GetMonitors();
        List<PublisherBase> GetPublishers();
    }
}
