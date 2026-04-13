using System.Collections.Generic;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Implementations;

namespace Kythr.Library.Monitoring.Abstractions
{
    public interface IStackMonitoring
    {
        IStackMonitoring AddMonitoring(ServiceHealthCheck monitor);
        IStackMonitoring AddCustomHealthCheck(IHealthCheck healthCheck, string name, IEnumerable<string> tags);
        IStackMonitoring AddPublishing(AlertTransportSettings alertTransportSettings, ServiceHealthCheck monitor);
        IStackMonitoring AddCustomPublisher(PublisherBase publisher);
        CustomMonitoring GetCustomMonitor(string name);
        List<ServiceMonitoringBase> GetMonitors();
        List<PublisherBase> GetPublishers();
    }
}
