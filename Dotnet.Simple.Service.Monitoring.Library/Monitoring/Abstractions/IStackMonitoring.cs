using Dotnet.Simple.Service.Monitoring.Library.Models;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public interface IStackMonitoring
    {
        void AddMonitoring(ServiceHealthCheck monitor);
    }
}
