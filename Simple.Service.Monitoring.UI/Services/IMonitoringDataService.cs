using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Services
{
    public interface IMonitoringDataService
    {
        Task<Models.HealthReport> GetHealthCheckReport();
        Task<HealthStatus> GetOverallStatus();
        Task<Dictionary<string, List<HealthCheckTimelineSegment>>> GetHealthCheckTimeline(int hours = 24);
        Task SendHealthCheckTimeline(int hours = 24);
        Task<Models.HealthReport> GetHealthReportByDateRange(DateTime from, DateTime to);
        Task AddHealthReport(HealthReport report);
        Task AddHealthCheckData(HealthCheckData healthCheckData);
        Task AddHealthChecksData(List<HealthCheckData> healthChecksData);
        void Init();
    }
}