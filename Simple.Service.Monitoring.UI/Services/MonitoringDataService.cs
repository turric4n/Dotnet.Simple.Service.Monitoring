using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.UI.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Services
{
    public class MonitoringDataService : IMonitoringDataService, IReportObserver, IDisposable
    {
        // Simulated database
        private readonly List<HealthCheckData> _healthCheckDatabase = new();
        private readonly IReportObservable _reportObservable;
        private readonly IHubContext<MonitoringHub> _hubContext;

        public MonitoringDataService(
            IReportObservable reportObservable,
            IHubContext<MonitoringHub> hubContext)
        {
            _reportObservable = reportObservable;
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
        /// Get timeline segments showing status changes for each service within a time window
        /// </summary>
        public Dictionary<string, List<HealthCheckTimelineSegment>> GetHealthCheckTimeline(int hours = 24)
        {
            var endTime = DateTime.UtcNow;
            var startTime = endTime.AddHours(-hours);
            
            var result = new Dictionary<string, List<HealthCheckTimelineSegment>>();
            
            // Group by service name
            var groupedChecks = _healthCheckDatabase.GroupBy(hc => hc.Name);
            
            foreach (var group in groupedChecks)
            {
                var serviceName = group.Key;
                if (string.IsNullOrEmpty(serviceName)) continue;
                
                var serviceChecks = group.OrderBy(c => c.LastUpdated).ToList();
                var segments = new List<HealthCheckTimelineSegment>();
                
                // Find the last check before our window starts
                var initialStatus = "Unknown";
                var checksBeforeWindow = serviceChecks.Where(c => c.LastUpdated < startTime)
                    .OrderByDescending(c => c.LastUpdated).FirstOrDefault();
                
                if (checksBeforeWindow != null)
                {
                    initialStatus = checksBeforeWindow.Status.ToString() ?? "Unknown";
                }
                
                string lastStatus = initialStatus;
                DateTime lastTimestamp = startTime;
                
                // Get checks within our time window
                var checksInWindow = serviceChecks
                    .Where(c => c.LastUpdated >= startTime && c.LastUpdated <= endTime)
                    .ToList();
                
                // If no checks in window, just add one segment with initial status
                if (!checksInWindow.Any())
                {
                    segments.Add(new HealthCheckTimelineSegment
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = initialStatus,
                        UptimePercentage = initialStatus == "Healthy" ? 100 : 0
                    });
                    
                    result[serviceName] = segments;
                    continue;
                }
                
                // Create segments for each status change
                foreach (var check in checksInWindow)
                {
                    var currentStatus = check.Status.ToString() ?? "Unknown";
                    
                    if (currentStatus != lastStatus)
                    {
                        segments.Add(new HealthCheckTimelineSegment
                        {
                            StartTime = lastTimestamp,
                            EndTime = check.LastUpdated,
                            Status = lastStatus
                        });
                        
                        lastStatus = currentStatus;
                        lastTimestamp = check.LastUpdated;
                    }
                }
                
                // Add final segment
                segments.Add(new HealthCheckTimelineSegment
                {
                    StartTime = lastTimestamp,
                    EndTime = endTime,
                    Status = lastStatus
                });
                
                // Calculate uptime percentage
                var totalDuration = (endTime - startTime).TotalSeconds;
                var healthyDuration = segments
                    .Where(s => s.Status == "Healthy")
                    .Sum(s => (s.EndTime - s.StartTime).TotalSeconds);
                
                var uptimePercentage = totalDuration > 0 
                    ? Math.Round((healthyDuration / totalDuration) * 100, 2)
                    : 0;
                
                // Add uptime percentage to first segment
                if (segments.Any())
                {
                    segments[0].UptimePercentage = uptimePercentage;
                }
                
                result[serviceName] = segments;
            }
            
            return result;
        }
        
        /// <summary>
        /// Send timeline data to clients
        /// </summary>
        public async Task SendHealthCheckTimeline(int hours = 24)
        {
            if (_hubContext != null)
            {
                var timeline = GetHealthCheckTimeline(hours);
                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksTimeline", timeline);
            }
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
                
                // Also send updated timeline data
                await SendHealthCheckTimeline(24); // Default to 24 hours
            }
        }

        public async Task AddHealthCheckData(HealthCheckData healthCheckData)
        {
            var previousStatus = GetOverallStatus();

            _healthCheckDatabase.Add(healthCheckData);

            var currentStatus = GetOverallStatus();

            if (_hubContext != null)
            {
                if (previousStatus != currentStatus)
                    await NotifyStatusChange(previousStatus, currentStatus);

                // Notify clients with the full report
                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksReport", GetHealthCheckReport());
                
                // Also send updated timeline data
                await SendHealthCheckTimeline(24); // Default to 24 hours
            }
        }

        public void Init()
        {
            _reportObservable.Subscribe(this);
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

        public void Dispose()
        {
            //
        }
    }

    // New model for timeline segments
    public class HealthCheckTimelineSegment
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public double UptimePercentage { get; set; }
    }
}
