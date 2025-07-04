using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.UI.Models;

namespace Simple.Service.Monitoring.UI.Repositories.Memory
{
    public class InMemoryMonitoringDataRepository : IMonitoringDataRepository
    {
        private readonly List<HealthCheckData> _healthCheckStore = new();
        private readonly List<HealthCheckTimeSeriesPoint> _timeSeriesStore = new();
        private readonly List<HealthCheckTimeRange> _timeRangeStore = new();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public async Task<HealthCheckData> GetLatestHealthCheckAsync(string name, string machineName)
        {
            try
            {
                _lock.EnterReadLock();
                var latestCheck = _healthCheckStore
                    .Where(hc => hc.Name == name && hc.MachineName == machineName)
                    .OrderByDescending(hc => hc.LastUpdated)
                    .FirstOrDefault();

                return latestCheck;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task AddHealthCheckDataAsync(HealthCheckData healthCheckData)
        {
            if (healthCheckData == null)
                throw new ArgumentNullException(nameof(healthCheckData));

            try
            {
                _lock.EnterWriteLock();
                
                // Find the latest check for this name and machine
                var latestCheck = _healthCheckStore
                    .Where(hc => hc.Name == healthCheckData.Name && hc.MachineName == healthCheckData.MachineName)
                    .OrderByDescending(hc => hc.LastUpdated)
                    .FirstOrDefault();

                // If the latest check exists and has the same status, just update LastUpdated
                if (latestCheck != null && latestCheck.Status == healthCheckData.Status)
                {
                    latestCheck.LastUpdated = DateTime.Now;
                    latestCheck.Duration = healthCheckData.Duration;
                    latestCheck.Description = healthCheckData.Description;
                    latestCheck.CheckError = healthCheckData.CheckError;
                }
                // Otherwise, add a new record
                else
                {
                    _healthCheckStore.Add(healthCheckData);
                }
                
                return Task.CompletedTask;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task AddHealthChecksDataAsync(IEnumerable<HealthCheckData> healthChecksData)
        {
            if (healthChecksData == null)
                throw new ArgumentNullException(nameof(healthChecksData));

            try
            {
                _lock.EnterWriteLock();
                
                // Define stale threshold
                var staleThreshold = TimeSpan.FromMinutes(1);
                var now = DateTime.Now;
                
                foreach (var data in healthChecksData)
                {
                    // Find the latest check for this name and machine
                    var latestCheck = _healthCheckStore
                        .Where(hc => hc.Name == data.Name && hc.MachineName == data.MachineName)
                        .OrderByDescending(hc => hc.LastUpdated)
                        .FirstOrDefault();

                    // Create the service key
                    var serviceKey = string.IsNullOrEmpty(data.MachineName)
                        ? data.Name
                        : $"{data.Name} ({data.MachineName})";
                        
                    // Find any open time range for this service
                    var openRange = _timeRangeStore
                        .FirstOrDefault(r => r.Name == data.Name && 
                                             r.MachineName == data.MachineName && 
                                             r.EndTime == null);
                        
                    // Handle time range updates
                    if (openRange != null)
                    {
                        // Check if the range is stale
                        bool isStale = openRange.IsStale(staleThreshold);
                        
                        // If status changed or is stale, close the current range
                        if (openRange.Status != data.Status || isStale)
                        {
                            // If stale, close with last update time + threshold
                            var closeTime = isStale
                                ? openRange.UpdateTime.Add(staleThreshold)
                                : now;
                            
                            // Close the existing range
                            openRange.EndTime = closeTime;
                            openRange.UpdateTime = now;
                            
                            // If stale, create an "Unknown" range between stale time and now
                            if (isStale)
                            {
                                var unknownRange = new HealthCheckTimeRange
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = data.Name,
                                    MachineName = data.MachineName ?? string.Empty,
                                    StartTime = closeTime,
                                    EndTime = now,
                                    UpdateTime = now,
                                    Status = HealthStatus.Unknown,
                                    StatusReason = "Status became unknown due to inactivity",
                                };
                                
                                _timeRangeStore.Add(unknownRange);
                            }
                            
                            // Create a new range with current status
                            var newRange = new HealthCheckTimeRange
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = data.Name,
                                MachineName = data.MachineName ?? string.Empty,
                                StartTime = now,
                                EndTime = null, // Still open
                                UpdateTime = now,
                                Status = data.Status,
                                StatusReason = data.Description,
                            };
                            
                            _timeRangeStore.Add(newRange);
                        }
                        else
                        {
                            // Status hasn't changed and not stale, just update UpdateTime
                            openRange.UpdateTime = now;
                        }
                    }
                    else
                    {
                        // No open range exists, create a new one
                        var newRange = new HealthCheckTimeRange
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = data.Name,
                            MachineName = data.MachineName ?? string.Empty,
                            StartTime = now,
                            EndTime = null, // Still open
                            UpdateTime = now,
                            Status = data.Status,
                            StatusReason = data.Description,
                        };
                        
                        _timeRangeStore.Add(newRange);
                    }

                    // Regular health check data update logic
                    if (latestCheck != null && latestCheck.Status == data.Status)
                    {
                        latestCheck.LastUpdated = now;
                        latestCheck.Duration = data.Duration;
                        latestCheck.Description = data.Description;
                        latestCheck.CheckError = data.CheckError;
                    }
                    else
                    {
                        _healthCheckStore.Add(data);

                        // Add time series point
                        _timeSeriesStore.Add(new HealthCheckTimeSeriesPoint
                        {
                            Name = data.Name,
                            MachineName = data.MachineName,
                            Timestamp = now,
                            Status = data.Status,
                            StatusReason = data.Description
                        });
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return Task.CompletedTask;
        }

        // Other methods remain unchanged...
        public Task<List<HealthCheckData>> GetLatestHealthChecksAsync()
        {
            try
            {
                _lock.EnterReadLock();
                var latestChecks = _healthCheckStore
                    .GroupBy(hc => new { hc.Name, hc.MachineName })
                    .Select(g => g.OrderByDescending(hc => hc.LastUpdated).First())
                    .ToList();

                return Task.FromResult(latestChecks);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<List<HealthCheckData>> GetHealthChecksByDateRangeAsync(DateTime from, DateTime to)
        {
            try
            {
                _lock.EnterReadLock();
                var checksInRange = _healthCheckStore
                    .Where(hc => hc.LastUpdated >= from && hc.LastUpdated <= to)
                    .GroupBy(hc => new { hc.Name, hc.MachineName })
                    .Select(g => g.OrderByDescending(hc => hc.LastUpdated).First())
                    .ToList();

                return Task.FromResult(checksInRange);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<IEnumerable<IGrouping<(string Name, string MachineName), HealthCheckData>>> GetGroupedHealthChecksAsync()
        {
            try
            {
                _lock.EnterReadLock();
                var groupedChecks = _healthCheckStore
                    .GroupBy(hc => (hc.Name, hc.MachineName))
                    .ToList();

                return Task.FromResult<IEnumerable<IGrouping<(string Name, string MachineName), HealthCheckData>>>(groupedChecks);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<IEnumerable<HealthCheckData>> GetHealthChecksInTimeWindowAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                _lock.EnterReadLock();
                var checksInWindow = _healthCheckStore
                    .Where(hc => hc.LastUpdated >= startTime && hc.LastUpdated <= endTime)
                    .ToList();

                return Task.FromResult<IEnumerable<HealthCheckData>>(checksInWindow);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task AddTimeSeriesPointAsync(HealthCheckTimeSeriesPoint point)
        {
            if (point == null)
                throw new ArgumentNullException(nameof(point));

            try
            {
                //_lock.EnterWriteLock();
                _timeSeriesStore.Add(point);
                return Task.CompletedTask;
            }
            finally
            {
                //_lock.ExitWriteLock();
            }
        }

        public Task AddTimeSeriesPointsAsync(IEnumerable<HealthCheckTimeSeriesPoint> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            try
            {
                //_lock.EnterWriteLock();
                _timeSeriesStore.AddRange(points);
                return Task.CompletedTask;
            }
            finally
            {
                //_lock.ExitWriteLock();
            }
        }

        public Task<List<HealthCheckTimeSeriesPoint>> GetTimeSeriesPointsAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                _lock.EnterReadLock();
                var points = _timeSeriesStore
                    .Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime)
                    .ToList();

                return Task.FromResult(points);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<Dictionary<string, List<HealthCheckTimeSeriesPoint>>> GetGroupedTimeSeriesPointsAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                _lock.EnterReadLock();
                var result = _timeSeriesStore
                    .Where(p => p.Timestamp >= startTime && p.Timestamp <= endTime)
                    .GroupBy(p => p.ServiceKey)
                    .ToDictionary(
                        g => g.Key,
                        g => g.ToList()
                    );

                return Task.FromResult(result);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task AddHealthCheckStatusChangeAsync(string name, string machineName, DateTime timestamp, HealthStatus status,
            string statusReason)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            try
            {
                _lock.EnterWriteLock();
                
                // Create the service key
                var serviceKey = string.IsNullOrEmpty(machineName)
                    ? name
                    : $"{name} ({machineName})";
                    
                // Find any open time range for this service
                var openRange = _timeRangeStore
                    .FirstOrDefault(r => r.Name == name && r.MachineName == machineName && r.EndTime == null);
                    
                // Close the existing time range if it exists and status is different
                if (openRange != null)
                {
                    // Only close if the status is changing
                    if (openRange.Status != status)
                    {
                        openRange.EndTime = timestamp;
                    }
                    else
                    {
                        // Same status, no need to create a new range
                        return Task.CompletedTask;
                    }
                }
                
                // Create a new time range with the current status
                var newRange = new HealthCheckTimeRange
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = name,
                    MachineName = machineName ?? string.Empty,
                    StartTime = timestamp,
                    EndTime = null, // Still open
                    Status = status,
                    StatusReason = statusReason,
                };
                
                _timeRangeStore.Add(newRange);
                
                return Task.CompletedTask;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task<List<HealthCheckTimeRange>> GetHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                _lock.EnterReadLock();
                
                // Find all time ranges that overlap with the specified time window
                var ranges = _timeRangeStore
                    .Where(r => 
                        // Range starts within our window
                        (r.StartTime >= startTime && r.StartTime <= endTime) ||
                        // Range ends within our window
                        (r.EndTime.HasValue && r.EndTime.Value >= startTime && r.EndTime.Value <= endTime) ||
                        // Range completely contains our window
                        (r.StartTime <= startTime && (!r.EndTime.HasValue || r.EndTime.Value >= endTime)))
                    .OrderBy(r => r.Name)
                    .ThenBy(r => r.MachineName)
                    .ThenBy(r => r.StartTime)
                    .ToList();
                
                return Task.FromResult(ranges);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<Dictionary<string, List<HealthCheckTimeRange>>> GetGroupedHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                _lock.EnterReadLock();
                
                // Find all time ranges that overlap with the specified time window
                var ranges = _timeRangeStore
                    .Where(r => 
                        // Range starts within our window
                        (r.StartTime >= startTime && r.StartTime <= endTime) ||
                        // Range ends within our window
                        (r.EndTime.HasValue && r.EndTime.Value >= startTime && r.EndTime.Value <= endTime) ||
                        // Range completely contains our window
                        (r.StartTime <= startTime && (!r.EndTime.HasValue || r.EndTime.Value >= endTime)))
                    .ToList();
                
                // Group by ServiceKey
                var result = ranges
                    .GroupBy(r => r.ServiceKey)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(r => r.StartTime).ToList()
                    );
                    
                return Task.FromResult(result);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}
