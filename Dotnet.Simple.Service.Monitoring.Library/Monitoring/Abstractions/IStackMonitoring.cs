using System.Collections.Generic;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public interface IStackMonitoring
    {
        IStackMonitoring AddMonitoring(ServiceHealthCheck monitor);
        IStackMonitoring AddPublishing(AlertTransportSettings alertTransportSettings, ServiceHealthCheck monitor);

        List<ServiceMonitoringBase> GetMonitors();
        List<PublisherBase> GetPublishers();
    }
}
