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
        // Simulated database
        private readonly List<HealthCheckData> _healthCheckDatabase = new();
        private readonly IHubContext<MonitoringHub> _hubContext;

        public MonitoringDataService(
            IReportObservable reportObservable,
            IHubContext<MonitoringHub> hubContext)
        {
            reportObservable.Subscribe(this);
            _hubContext = hubContext;
        }

        /// <summary>
        /// Returns the latest health report (latest entry for each name)
        /// </summary>
        public Models.HealthReport GetHealthCheckReport()
        {
            var latestChecks = _healthCheckDatabase
                .GroupBy(hc => hc.Name)
                .Select(g => g.OrderByDescending(hc => hc.LastUpdated).First())
                .ToList();

            return new Models.HealthReport
            {
                Status = GetOverallStatus().ToString(),
                LastUpdated = latestChecks.Any() ? latestChecks.Max(hc => hc.LastUpdated) : DateTime.UtcNow,
                HealthChecks = latestChecks
            };
        }

        /// <summary>
        /// Returns a health report for entries between the given date range.
        /// </summary>
        public Models.HealthReport GetHealthReportByDateRange(DateTime from, DateTime to)
        {
            var checksInRange = _healthCheckDatabase
                .Where(hc => hc.LastUpdated >= from && hc.LastUpdated <= to)
                .GroupBy(hc => hc.Name)
                .Select(g => g.OrderByDescending(hc => hc.LastUpdated).First())
                .ToList();

            var status = HealthStatus.Healthy;
            if (checksInRange.Any(c => c.Status == HealthStatus.Unhealthy))
                status = HealthStatus.Unhealthy;
            else if (checksInRange.Any(c => c.Status == HealthStatus.Degraded))
                status = HealthStatus.Degraded;

            return new Models.HealthReport
            {
                Status = status.ToString(),
                LastUpdated = checksInRange.Any() ? checksInRange.Max(hc => hc.LastUpdated) : DateTime.UtcNow,
                HealthChecks = checksInRange
            };
        }

        /// <summary>
        /// Updates health report data and notifies clients if status changes.
        /// </summary>
        public async Task AddHealthReport(Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
        {
            var previousStatus = GetOverallStatus();

            foreach (var entry in report.Entries)
            {
                var healthCheckData = new HealthCheckData(entry.Value) { Name = entry.Key };
                _healthCheckDatabase.Add(healthCheckData);
            }

            var currentStatus = GetOverallStatus();

            if (_hubContext != null)
            {
                if (previousStatus != currentStatus)
                    await NotifyStatusChange(previousStatus, currentStatus);

                // Notify clients with the full report
                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksReport", GetHealthCheckReport());
            }
        }

        public HealthStatus GetOverallStatus()
        {
            var latestChecks = _healthCheckDatabase
                .GroupBy(hc => hc.Name)
                .Select(g => g.OrderByDescending(hc => hc.LastUpdated).First())
                .ToList();

            if (!latestChecks.Any())
                return HealthStatus.Unhealthy;

            if (latestChecks.Any(c => c.Status == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;

            if (latestChecks.Any(c => c.Status == HealthStatus.Degraded))
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
