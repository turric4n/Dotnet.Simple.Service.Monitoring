using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.UI.Services
{
    public interface IMonitoringDataService
    {
        Models.HealthReport GetHealthCheckReport();
        HealthStatus GetOverallStatus();
    }
}
