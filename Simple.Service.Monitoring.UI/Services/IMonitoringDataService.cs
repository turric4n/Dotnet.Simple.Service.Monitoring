﻿using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.UI.Services
{
    public interface IMonitoringDataService
    {
        // Métodos existentes
        Models.HealthReport GetHealthCheckReport();
        HealthStatus GetOverallStatus();

        // Nuevos métodos para timeline
        Dictionary<string, List<HealthCheckTimelineSegment>> GetHealthCheckTimeline(int hours = 24);
        Task SendHealthCheckTimeline(int hours = 24);
        Models.HealthReport GetHealthReportByDateRange(DateTime from, DateTime to);
        Task AddHealthReport(HealthReport report);
        Task AddHealthCheckData(HealthCheckData healthCheckData);
        Task AddHealthChecksData(List<HealthCheckData> healthChecksData);
        void Init();
    }
}
