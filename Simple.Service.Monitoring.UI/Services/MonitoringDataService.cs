using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.UI.Hubs;
using Simple.Service.Monitoring.UI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Services
{
    public class MonitoringDataService : IMonitoringDataService, IReportObserver
    {
        // Single HealthReport instance instead of a dictionary
        private Models.HealthReport _healthReport = new Models.HealthReport
        {
            Status = "Unknown",
            LastUpdated = DateTime.UtcNow,
            HealthChecks = new List<HealthCheckData>()
        };
        
        private readonly IHubContext<MonitoringHub> _hubContext;

        public MonitoringDataService(
            IReportObservable reportObservable,
            IHubContext<MonitoringHub> hubContext)
        {
            reportObservable.Subscribe(this);
            _hubContext = hubContext;
        }

        /// <summary>
        /// Returns the current health report
        /// </summary>
        public Models.HealthReport GetHealthCheckReport()
        {
            return _healthReport;
        }

        /// <summary>
        /// Updates health report data and notifies clients if status changes.
        /// </summary>
        public async Task AddHealthReport(Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
        {
            var statusChanged = false;
            var previousStatus = GetOverallStatus();
            var previousReportStatus = _healthReport.Status;

            // Convert incoming report to our model
            var newHealthReport = new Models.HealthReport
            {
                Status = report.Status.ToString(),
                LastUpdated = DateTime.UtcNow,
                TotalDuration = report.TotalDuration,
                HealthChecks = report.Entries.Select(entry => 
                    new HealthCheckData(entry.Value) { Name = entry.Key }
                ).ToList()
            };

            // Check if status changed
            if (previousReportStatus != newHealthReport.Status)
            {
                statusChanged = true;
            }

            // Update the health report
            _healthReport = newHealthReport;

            var currentStatus = GetOverallStatus();

            if (_hubContext != null)
            {
                await NotifyStatusChange(previousStatus, currentStatus);
                
                // Notify clients with the full report
                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksReport", GetHealthCheckReport());
            }
        }

        public HealthStatus GetOverallStatus()
        {
            // Convert string status from the report to HealthStatus enum
            if (_healthReport == null || string.IsNullOrEmpty(_healthReport.Status))
                return HealthStatus.Unhealthy;

            if (_healthReport.Status.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase))
                return HealthStatus.Unhealthy;

            if (_healthReport.Status.Equals("Degraded", StringComparison.OrdinalIgnoreCase))
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }

        // IReportObserver implementation
        public bool ExecuteAlways => true;

        public void OnCompleted() { /* No action needed */ }

        public void OnError(Exception error) { /* Optionally log error */ }

        public void OnNext(Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport value)
        {
            _ = AddHealthReport(value);
        }

        /// <summary>
        /// Notifies all SignalR clients about a status change.
        /// </summary>
        private async Task NotifyStatusChange(HealthStatus previousStatus, HealthStatus currentStatus)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveStatusChange",
                new
                {
                    PreviousStatus = previousStatus.ToString(),
                    CurrentStatus = currentStatus.ToString(),
                    LastUpdated = DateTime.UtcNow.ToString("o")
                });
        }
    }
}
