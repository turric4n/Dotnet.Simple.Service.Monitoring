using Microsoft.AspNetCore.SignalR;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.UI.Hubs;
using Simple.Service.Monitoring.UI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthStatus = Simple.Service.Monitoring.Library.Models.HealthStatus;

namespace Simple.Service.Monitoring.UI.Services
{
    public class MonitoringDataService : IMonitoringDataService, IReportObserver, IDisposable
    {
        private readonly IReportObservable _reportObservable;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly IMonitoringDataRepository _repository;

        public MonitoringDataService(
            IMonitoringDataRepositoryLocator monitoringDataRepositoryLocator,
            IReportObservable reportObservable,
            IHubContext<MonitoringHub> hubContext)
        {
            _repository = monitoringDataRepositoryLocator.GetMonitoringDataRepository();
            _reportObservable = reportObservable;
            _hubContext = hubContext;
        }

        /// <summary>
        /// Returns the latest health report (latest entry for each name and machine name combination)
        /// </summary>
        public async Task<Models.HealthReport> GetHealthCheckReport()
        {
            var latestChecks = await _repository.GetLatestHealthChecksAsync();

            return new Models.HealthReport
            {
                Status = (await GetOverallStatus()).ToString(),
                LastUpdated = latestChecks.Any() ? latestChecks.Max(hc => hc.LastUpdated) : DateTime.Now,
                HealthChecks = latestChecks
            };
        }

        /// <summary>
        /// Returns a health report for entries between the given date range.
        /// </summary>
        public async Task<Models.HealthReport> GetHealthReportByDateRange(DateTime from, DateTime to)
        {
            var checksInRange = await _repository.GetHealthChecksByDateRangeAsync(from, to);

            var status = HealthStatus.Healthy;
            if (checksInRange.Any(c => c.Status == HealthStatus.Unhealthy))
                status = HealthStatus.Unhealthy;
            else if (checksInRange.Any(c => c.Status == HealthStatus.Degraded))
                status = HealthStatus.Degraded;

            return new Models.HealthReport
            {
                Status = status.ToString(),
                LastUpdated = checksInRange.Any() ? checksInRange.Max(hc => hc.LastUpdated) : DateTime.Now,
                HealthChecks = checksInRange
            };
        }

        /// <summary>
        /// Get timeline segments showing status changes for each service within a time window
        /// </summary>
        public async Task<Dictionary<string, List<HealthCheckTimelineSegment>>> GetHealthCheckTimeline(int hours = 24)
        {
            var endTime = DateTime.Now;
            var startTime = endTime.AddHours(-hours);

            var result = new Dictionary<string, List<HealthCheckTimelineSegment>>();

            // Get all health checks grouped by name and machine name
            var groupedChecks = await _repository.GetGroupedHealthChecksAsync();

            foreach (var group in groupedChecks)
            {
                var serviceName = group.Key.Name;
                var machineName = group.Key.MachineName;
                if (string.IsNullOrEmpty(serviceName)) continue;

                // Create composite key for the dictionary
                var compositeKey = string.IsNullOrEmpty(machineName)
                    ? serviceName
                    : $"{serviceName} ({machineName})";

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

                    result[compositeKey] = segments;
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

                result[compositeKey] = segments;
            }

            return result;
        }

        public async Task<Dictionary<string, List<HealthCheckTimelineSegment>>> GetHealthCheckTimelineFromRanges(int hours = 24)
        {
            var endTime = DateTime.Now;
            var startTime = endTime.AddHours(-hours);
            
            // Define threshold after which health status is considered stale
            var staleThreshold = TimeSpan.FromMinutes(1);

            // Get time ranges directly from repository
            var timeRanges = await _repository.GetGroupedHealthCheckTimeRangesAsync(startTime, endTime);
            var result = new Dictionary<string, List<HealthCheckTimelineSegment>>();

            foreach (var kvp in timeRanges)
            {
                string serviceKey = kvp.Key;
                var ranges = kvp.Value.OrderBy(r => r.StartTime).ToList();
                var segments = new List<HealthCheckTimelineSegment>();

                if (!ranges.Any())
                {
                    // If no data, add "Unknown" segment for the whole period
                    segments.Add(new HealthCheckTimelineSegment
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = "Unknown",
                        UptimePercentage = 0
                    });

                    result[serviceKey] = segments;
                    continue;
                }

                // Handle gap at the beginning if needed
                if (ranges.First().StartTime > startTime)
                {
                    segments.Add(new HealthCheckTimelineSegment
                    {
                        StartTime = startTime,
                        EndTime = ranges.First().StartTime,
                        Status = "Unknown"
                    });
                }

                // Convert each time range to a segment
                foreach (var range in ranges)
                {
                    // Check if this is a current range that might be stale
                    if (range.EndTime == null)
                    {
                        // Use the IsStale method which checks against UpdateTime
                        if (range.IsStale(staleThreshold))
                        {
                            // For stale ranges, we show the status up to the last update time plus threshold
                            var staleStart = range.UpdateTime.Add(staleThreshold);
                            
                            // Add a segment with the known status up to the stale point
                            segments.Add(new HealthCheckTimelineSegment
                            {
                                StartTime = range.StartTime,
                                EndTime = staleStart,
                                Status = range.Status.ToString()
                            });
                            
                            // Add another segment with "Unknown" status after the stale point
                            segments.Add(new HealthCheckTimelineSegment
                            {
                                StartTime = staleStart,
                                EndTime = endTime,
                                Status = "Unknown"
                            });
                        }
                        else
                        {
                            // Not stale yet, use as is up to current time
                            segments.Add(new HealthCheckTimelineSegment
                            {
                                StartTime = range.StartTime,
                                EndTime = endTime,
                                Status = range.Status.ToString()
                            });
                        }
                    }
                    else
                    {
                        // Closed range, use as is
                        segments.Add(new HealthCheckTimelineSegment
                        {
                            StartTime = range.StartTime,
                            EndTime = range.EndTime.Value,
                            Status = range.Status.ToString()
                        });
                    }
                }

                // Handle gaps between segments if any
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    if (segments[i].EndTime < segments[i + 1].StartTime)
                    {
                        segments.Insert(i + 1, new HealthCheckTimelineSegment
                        {
                            StartTime = segments[i].EndTime,
                            EndTime = segments[i + 1].StartTime,
                            Status = "Unknown"
                        });
                        i++; // Skip the newly inserted segment
                    }
                }

                // Calculate uptime percentage
                var totalDuration = (endTime - startTime).TotalSeconds;
                var healthyDuration = segments
                    .Where(s => s.Status == "Healthy")
                    .Sum(s => ((s.EndTime == default ? DateTime.Now : s.EndTime) - s.StartTime).TotalSeconds);

                var uptimePercentage = totalDuration > 0
                    ? Math.Round((healthyDuration / totalDuration) * 100, 2)
                    : 0;

                // Add uptime percentage to first segment
                if (segments.Any())
                {
                    segments[0].UptimePercentage = uptimePercentage;
                }

                result[serviceKey] = segments;
            }

            return result;
        }

        public async Task SendHealthCheckTimeline(int hours = 24)
        {
            if (_hubContext != null)
            {
                var timeline = await GetHealthCheckTimelineFromRanges(hours);
                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksTimeline", timeline);
            }
        }   

        public async Task AddHealthReport(Microsoft.Extensions.Diagnostics.HealthChecks.HealthReport report)
        {
            var previousStatus = await GetOverallStatus();
            var healthChecksToAdd = new List<HealthCheckData>();

            foreach (var entry in report.Entries)
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                healthChecksToAdd.Add(healthCheckData);

                // Check for status changes
                var latestCheck = await _repository.GetLatestHealthCheckAsync(
                    healthCheckData.Name, healthCheckData.MachineName);

                if (latestCheck == null || latestCheck.Status != healthCheckData.Status)
                {
                    // Status changed or first check, add a new status change
                    await _repository.AddHealthCheckStatusChangeAsync(
                        healthCheckData.Name,
                        healthCheckData.MachineName,
                        healthCheckData.LastUpdated,
                        healthCheckData.Status,
                        healthCheckData.Description
                    );
                }
            }

            await _repository.AddHealthChecksDataAsync(healthChecksToAdd);
            var currentStatus = await GetOverallStatus();

            if (_hubContext != null)
            {
                if (previousStatus != currentStatus)
                    await NotifyStatusChange(previousStatus, currentStatus);

                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksReport", await GetHealthCheckReport());
                await SendHealthCheckTimeline(24);
            }
        }

        public async Task AddHealthCheckData(HealthCheckData healthCheckData)
        {
            var previousStatus = await GetOverallStatus();

            // Get the current status for this service
            var latestCheck = await _repository.GetLatestHealthCheckAsync(
                healthCheckData.Name, healthCheckData.MachineName);

            // Add to regular health checks
            await _repository.AddHealthCheckDataAsync(healthCheckData);

            // The repository now handles UpdateTime - we only need to add a status change
            // if the status actually changed or there's no existing check
            if (latestCheck == null || latestCheck.Status != healthCheckData.Status)
            {
                // Status changed or first check, add a new status change
                await _repository.AddHealthCheckStatusChangeAsync(
                    healthCheckData.Name,
                    healthCheckData.MachineName,
                    healthCheckData.LastUpdated,
                    healthCheckData.Status,
                    healthCheckData.Description
                );
            }
            // No need to update if same status - the repository will handle updating UpdateTime

            var currentStatus = await GetOverallStatus();

            if (_hubContext != null)
            {
                if (previousStatus != currentStatus)
                    await NotifyStatusChange(previousStatus, currentStatus);

                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksReport", await GetHealthCheckReport());
                await SendHealthCheckTimeline(24);
            }
        }

        public async Task AddHealthChecksData(List<HealthCheckData> healthChecksData)
        {
            if (healthChecksData == null || !healthChecksData.Any())
                return;

            var previousStatus = await GetOverallStatus();

            // Process each health check separately to track status changes
            foreach (var healthCheckData in healthChecksData)
            {
                var latestCheck = await _repository.GetLatestHealthCheckAsync(
                    healthCheckData.Name, healthCheckData.MachineName);

                if (latestCheck == null || latestCheck.Status != healthCheckData.Status)
                {
                    // Status changed or first check, add a new status change
                    await _repository.AddHealthCheckStatusChangeAsync(
                        healthCheckData.Name,
                        healthCheckData.MachineName,
                        healthCheckData.LastUpdated,
                        (HealthStatus)healthCheckData.Status,
                        healthCheckData.Description
                    );
                }
            }

            // Add to regular health checks
            await _repository.AddHealthChecksDataAsync(healthChecksData);

            var currentStatus = await GetOverallStatus();

            if (_hubContext != null)
            {
                if (previousStatus != currentStatus)
                    await NotifyStatusChange(previousStatus, currentStatus);

                await _hubContext.Clients.All.SendAsync("ReceiveHealthChecksReport", await GetHealthCheckReport());
                await SendHealthCheckTimeline(24);
            }
        }

        public void Init()
        {
            _reportObservable.Subscribe(this);
        }

        public async Task<HealthStatus> GetOverallStatus()
        {
            var latestChecks = await _repository.GetLatestHealthChecksAsync();

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

        private async Task NotifyStatusChange(HealthStatus previousStatus, HealthStatus currentStatus)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveStatusChange",
                new
                {
                    PreviousStatus = previousStatus.ToString(),
                    CurrentStatus = currentStatus.ToString(),
                    LastUpdated = DateTime.Now.ToString("o") // Use ISO 8601 format with local time
                });
        }

        private async Task<string> DetermineInitialStatus(string serviceKey, DateTime startTime)
        {
            // Implement logic to find the last status before startTime
            // This might require an additional repository method
            // For now returning "Unknown" as default
            return "Unknown";
        }

        public void Dispose()
        {
            // Dispose resources if needed
        }
    }

    public class HealthCheckTimelineSegment
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
        public double UptimePercentage { get; set; }
    }
}