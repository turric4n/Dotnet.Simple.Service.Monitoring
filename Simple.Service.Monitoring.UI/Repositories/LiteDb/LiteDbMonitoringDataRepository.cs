using LiteDB;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Simple.Service.Monitoring.UI.Models;

namespace Simple.Service.Monitoring.UI.Repositories.LiteDb
{
    public class LiteDbMonitoringDatarepository : IMonitoringDataRepository, IDisposable
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<HealthCheckData> _collection;
        private readonly ILiteCollection<HealthCheckTimeSeriesPoint> _timeSeriesCollection;
        private readonly ILiteCollection<HealthCheckTimeRange> _timeRangesCollection;

        private readonly object _lock = new object();
        
        public LiteDbMonitoringDatarepository()
        {
            // Set default database path if not provided
            var dbPath = "monitoring-data.db";

            // Create connection to LiteDB
            _database = new LiteDatabase($"Filename={dbPath};Connection=shared");
            
            // Get collections
            _collection = _database.GetCollection<HealthCheckData>("healthchecks");
            _timeSeriesCollection = _database.GetCollection<HealthCheckTimeSeriesPoint>("timeseries");
            _timeRangesCollection = _database.GetCollection<HealthCheckTimeRange>("timeranges");
            
            // Create indexes for faster queries
            _collection.EnsureIndex(x => x.Name);
            _collection.EnsureIndex(x => x.MachineName);
            _collection.EnsureIndex(x => x.LastUpdated);
            _collection.EnsureIndex(x => new { x.Name, x.MachineName });
            
            // Create indexes for time series collection
            _timeSeriesCollection.EnsureIndex(x => x.Name);
            _timeSeriesCollection.EnsureIndex(x => x.MachineName);
            _timeSeriesCollection.EnsureIndex(x => x.Timestamp);
            _timeSeriesCollection.EnsureIndex(x => x.ServiceKey);
            _timeSeriesCollection.EnsureIndex(x => new { x.Name, x.MachineName });
            
            // Create indexes for time ranges collection
            _timeRangesCollection.EnsureIndex(x => x.Name);
            _timeRangesCollection.EnsureIndex(x => x.MachineName);
            _timeRangesCollection.EnsureIndex(x => x.StartTime);
            _timeRangesCollection.EnsureIndex(x => x.EndTime);
            _timeRangesCollection.EnsureIndex(x => x.Status);
            _timeRangesCollection.EnsureIndex(x => x.ServiceKey);
        }

        public Task<HealthCheckData> GetLatestHealthCheckAsync(string name, string machineName)
        {
            lock (_lock)
            {
                var latestCheck = _collection
                    .Find(hc => hc.Name == name && hc.MachineName == machineName)
                    .OrderByDescending(hc => hc.LastUpdated)
                    .FirstOrDefault();

                return Task.FromResult(latestCheck);
            }
        }

        public Task AddHealthCheckStatusChangeAsync(string name, string machineName, DateTime timestamp, HealthStatus status, string statusReason)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            lock (_lock)
            {
                // Calculate the service key
                var serviceKey = string.IsNullOrEmpty(machineName)
                    ? name
                    : $"{name} ({machineName})";

                // Find any open time range for this service
                var openRange = _timeRangesCollection
                    .Find(r => r.Name == name && r.MachineName == machineName && r.EndTime == null)
                    .FirstOrDefault();

                // Close the existing time range if status is different
                if (openRange != null)
                {
                    // Only close if the status is changing
                    if (openRange.Status != status)
                    {
                        openRange.EndTime = timestamp;
                        _timeRangesCollection.Update(openRange);
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
                    // CRITICAL: Ensure ID is set before inserting into LiteDB
                    Id = ObjectId.NewObjectId().ToString(),
                    Name = name,
                    MachineName = machineName ?? string.Empty,
                    StartTime = timestamp,
                    EndTime = null, // Still open
                    Status = status,
                    StatusReason = statusReason,
                };

                _timeRangesCollection.Insert(newRange);

                return Task.CompletedTask;
            }
        }

        // Modify AddHealthCheckDataAsync to ensure IDs are set
        public async Task AddHealthCheckDataAsync(HealthCheckData healthCheckData)
        {
            if (healthCheckData == null)
                throw new ArgumentNullException(nameof(healthCheckData));

            lock (_lock)
            {
                // Ensure ID is set
                if (string.IsNullOrEmpty(healthCheckData.Id))
                {
                    healthCheckData.Id = ObjectId.NewObjectId().ToString();
                }

                // Ensure CreationDate is set
                if (healthCheckData.CreationDate == default)
                {
                    healthCheckData.CreationDate = DateTime.Now;
                }

                // Find the latest check for this name and machine
                var latestCheck = _collection
                    .Find(hc => hc.Name == healthCheckData.Name && hc.MachineName == healthCheckData.MachineName)
                    .OrderByDescending(hc => hc.LastUpdated)
                    .FirstOrDefault();

                // If the latest check exists and has the same status, just update LastUpdated
                if (latestCheck != null && latestCheck.Status == healthCheckData.Status)
                {
                    latestCheck.LastUpdated = DateTime.Now;
                    latestCheck.Duration = healthCheckData.Duration;
                    latestCheck.Description = healthCheckData.Description;
                    latestCheck.CheckError = healthCheckData.CheckError;

                    _collection.Update(latestCheck);
                }
                // Otherwise, insert a new record
                else
                {
                    _collection.Insert(healthCheckData);
                }
            }

            await Task.CompletedTask;
        }

        // Update AddHealthChecksDataAsync to ensure IDs are set and handle time range updates
        public async Task AddHealthChecksDataAsync(IEnumerable<HealthCheckData> healthChecksData)
        {
            if (healthChecksData == null)
                throw new ArgumentNullException(nameof(healthChecksData));

            lock (_lock)
            {
                // Convert to list to avoid multiple enumeration
                var dataList = healthChecksData.ToList();
                
                // Define stale threshold
                var staleThreshold = TimeSpan.FromMinutes(1);
                var now = DateTime.Now;
                
                // Ensure IDs and CreationDate are set on all items
                foreach (var data in dataList)
                {
                    if (string.IsNullOrEmpty(data.Id))
                    {
                        data.Id = ObjectId.NewObjectId().ToString();
                    }

                    if (data.CreationDate == default)
                    {
                        data.CreationDate = now;
                    }
                }

                // Process each item individually to check status changes
                foreach (var data in dataList)
                {
                    // Find the latest check for this name and machine
                    var latestCheck = _collection
                        .Find(hc => hc.Name == data.Name && hc.MachineName == data.MachineName)
                        .OrderByDescending(hc => hc.LastUpdated)
                        .FirstOrDefault();

                    // Find any open time range for this service
                    var openRange = _timeRangesCollection
                        .Find(r => r.Name == data.Name && r.MachineName == data.MachineName && r.EndTime == null)
                        .FirstOrDefault();
                    
                    // Calculate the service key
                    var serviceKey = string.IsNullOrEmpty(data.MachineName)
                        ? data.Name
                        : $"{data.Name} ({data.MachineName})";
                    
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
                            _timeRangesCollection.Update(openRange);
                            
                            // If stale, create an "Unknown" range between stale time and now
                            if (isStale)
                            {
                                var unknownRange = new HealthCheckTimeRange
                                {
                                    Id = ObjectId.NewObjectId().ToString(),
                                    Name = data.Name,
                                    MachineName = data.MachineName ?? string.Empty,
                                    StartTime = closeTime,
                                    EndTime = now,
                                    UpdateTime = now,
                                    Status = HealthStatus.Unknown,
                                    StatusReason = "Status became unknown due to inactivity",
                                };
                                
                                _timeRangesCollection.Insert(unknownRange);
                            }
                            
                            // Create a new range with current status
                            var newRange = new HealthCheckTimeRange
                            {
                                Id = ObjectId.NewObjectId().ToString(),
                                Name = data.Name,
                                MachineName = data.MachineName ?? string.Empty,
                                StartTime = now,
                                EndTime = null, // Still open
                                UpdateTime = now,
                                Status = data.Status,
                                StatusReason = data.Description,
                            };
                            
                            _timeRangesCollection.Insert(newRange);
                        }
                        else
                        {
                            // Status hasn't changed and not stale, just update UpdateTime
                            openRange.UpdateTime = now;
                            _timeRangesCollection.Update(openRange);
                        }
                    }
                    else
                    {
                        // No open range exists, create a new one
                        var newRange = new HealthCheckTimeRange
                        {
                            Id = ObjectId.NewObjectId().ToString(),
                            Name = data.Name,
                            MachineName = data.MachineName ?? string.Empty,
                            StartTime = now,
                            EndTime = null, // Still open
                            UpdateTime = now,
                            Status = data.Status,
                            StatusReason = data.Description,
                        };
                        
                        _timeRangesCollection.Insert(newRange);
                    }

                    // Regular health check data update logic
                    if (latestCheck != null && latestCheck.Status == data.Status)
                    {
                        latestCheck.LastUpdated = now;
                        latestCheck.Duration = data.Duration;
                        latestCheck.Description = data.Description;
                        latestCheck.CheckError = data.CheckError;

                        _collection.Update(latestCheck);
                    }
                    else
                    {
                        _collection.Insert(data);
                    }
                }
            }

            await Task.CompletedTask;
        }

        // Other methods remain unchanged...
        public Task<List<HealthCheckData>> GetLatestHealthChecksAsync()
        {
            lock (_lock)
            {
                // Get all documents
                var allDocs = _collection.FindAll().ToList();
                
                // Group and select latest by LastUpdated
                var latestChecks = allDocs
                    .GroupBy(hc => new { hc.Name, hc.MachineName })
                    .Select(g => g.OrderByDescending(hc => hc.LastUpdated).First())
                    .ToList();
                
                return Task.FromResult(latestChecks);
            }
        }

        public Task<List<HealthCheckData>> GetHealthChecksByDateRangeAsync(DateTime from, DateTime to)
        {
            lock (_lock)
            {
                // Get documents in date range
                var checksInRange = _collection
                    .Find(hc => hc.LastUpdated >= from && hc.LastUpdated <= to)
                    .ToList()
                    .GroupBy(hc => new { hc.Name, hc.MachineName })
                    .Select(g => g.OrderByDescending(hc => hc.LastUpdated).First())
                    .ToList();
                
                return Task.FromResult(checksInRange);
            }
        }

        public Task<IEnumerable<IGrouping<(string Name, string MachineName), HealthCheckData>>> GetGroupedHealthChecksAsync()
        {
            lock (_lock)
            {
                // Get all documents and group them
                var allDocs = _collection.FindAll().ToList();
                var groupedChecks = allDocs
                    .GroupBy(hc => (hc.Name, hc.MachineName))
                    .ToList();
                
                return Task.FromResult<IEnumerable<IGrouping<(string Name, string MachineName), HealthCheckData>>>(groupedChecks);
            }
        }

        public Task<IEnumerable<HealthCheckData>> GetHealthChecksInTimeWindowAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                // Get documents in time window
                var checksInWindow = _collection
                    .Find(hc => hc.LastUpdated >= startTime && hc.LastUpdated <= endTime)
                    .ToList();
                
                return Task.FromResult<IEnumerable<HealthCheckData>>(checksInWindow);
            }
        }

        public Task AddTimeSeriesPointAsync(HealthCheckTimeSeriesPoint point)
        {
            if (point == null)
                throw new ArgumentNullException(nameof(point));

            lock (_lock)
            {
                // Generate an ID if not provided
                if (string.IsNullOrEmpty(point.Id))
                {
                    point.Id = ObjectId.NewObjectId().ToString();
                }

                _timeSeriesCollection.Insert(point);
                return Task.CompletedTask;
            }
        }

        public Task AddTimeSeriesPointsAsync(IEnumerable<HealthCheckTimeSeriesPoint> points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            lock (_lock)
            {
                var pointsList = points.ToList();
                
                // Generate IDs for points that don't have them
                foreach (var point in pointsList.Where(p => string.IsNullOrEmpty(p.Id)))
                {
                    point.Id = ObjectId.NewObjectId().ToString();
                }

                _timeSeriesCollection.InsertBulk(pointsList);
                return Task.CompletedTask;
            }
        }

        public Task<List<HealthCheckTimeSeriesPoint>> GetTimeSeriesPointsAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                var points = _timeSeriesCollection
                    .Find(p => p.Timestamp >= startTime && p.Timestamp <= endTime)
                    .OrderBy(p => p.Timestamp)
                    .ToList();
                
                return Task.FromResult(points);
            }
        }

        public Task<Dictionary<string, List<HealthCheckTimeSeriesPoint>>> GetGroupedTimeSeriesPointsAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                // Get all points in the time range
                var points = _timeSeriesCollection
                    .Find(p => p.Timestamp >= startTime && p.Timestamp <= endTime)
                    .ToList();
                
                // Group by ServiceKey
                var result = points
                    .GroupBy(p => p.ServiceKey)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(p => p.Timestamp).ToList()
                    );
                
                return Task.FromResult(result);
            }
        }

        public Task<List<HealthCheckTimeRange>> GetHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                // Query for time ranges that overlap with the specified window
                var ranges = _timeRangesCollection
                    .Find(r => 
                        // Range starts within our window
                        (r.StartTime >= startTime && r.StartTime <= endTime) ||
                        // Range ends within our window
                        (r.EndTime != null && r.EndTime >= startTime && r.EndTime <= endTime) ||
                        // Range completely contains our window
                        (r.StartTime <= startTime && (r.EndTime == null || r.EndTime >= endTime)))
                    .OrderBy(r => r.Name)
                    .ThenBy(r => r.MachineName)
                    .ThenBy(r => r.StartTime)
                    .ToList();
                
                    
                return Task.FromResult(ranges);
            }
        }

        public Task<Dictionary<string, List<HealthCheckTimeRange>>> GetGroupedHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime)
        {
            lock (_lock)
            {
                // Query for time ranges that overlap with the specified window
                var ranges = _timeRangesCollection
                    .Find(r => 
                        // Range starts within our window
                        (r.StartTime >= startTime && r.StartTime <= endTime) ||
                        // Range ends within our window
                        (r.EndTime != null && r.EndTime >= startTime && r.EndTime <= endTime) ||
                        // Range completely contains our window
                        (r.StartTime <= startTime && (r.EndTime == null || r.EndTime >= endTime)))
                    .OrderBy(r => r.StartTime)
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
        }

        public void Dispose()
        {
            _database?.Dispose();
        }
    }
}
