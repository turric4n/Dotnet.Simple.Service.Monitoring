using LiteDB;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Repositories.LiteDb
{
    public class LiteDbMonitoringDatarepository : IMonitoringDataRepository, IDisposable
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<HealthCheckData> _collection;

        private readonly object _lock = new object();
        
        public LiteDbMonitoringDatarepository()
        {
            // Set default database path if not provided
            var dbPath = "monitoring-data.db";

            try
            {
                // Create connection to LiteDB
                _database = new LiteDatabase($"Filename={dbPath};Connection=shared");
                
                // Get collection
                _collection = _database.GetCollection<HealthCheckData>("healthchecks");
                
                // Create indexes for faster queries
                _collection.EnsureIndex(x => x.Name);
                _collection.EnsureIndex(x => x.MachineName);
                _collection.EnsureIndex(x => x.LastUpdated);
                _collection.EnsureIndex(x => new { x.Name, x.MachineName });
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public Task<HealthCheckData> GetLatestHealthCheckAsync(string name, string machineName)
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task AddHealthCheckDataAsync(HealthCheckData healthCheckData)
        {
            if (healthCheckData == null)
                throw new ArgumentNullException(nameof(healthCheckData));

            try
            {
                lock (_lock)
                {
                    // Ensure CreationDate is set
                    if (healthCheckData.CreationDate == default)
                    {
                        healthCheckData.CreationDate = DateTime.UtcNow;
                    }

                    // Find the latest check for this name and machine
                    var latestCheck = _collection
                        .Find(hc => hc.Name == healthCheckData.Name && hc.MachineName == healthCheckData.MachineName)
                        .OrderByDescending(hc => hc.LastUpdated)
                        .FirstOrDefault();

                    // If the latest check exists and has the same status, just update LastUpdated
                    if (latestCheck != null && latestCheck.Status == healthCheckData.Status)
                    {
                        latestCheck.LastUpdated = DateTime.UtcNow;
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
                
                return;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task AddHealthChecksDataAsync(IEnumerable<HealthCheckData> healthChecksData)
        {
            if (healthChecksData == null)
                throw new ArgumentNullException(nameof(healthChecksData));

            try
            {
                lock (_lock)
                {
                    // Convert to list to avoid multiple enumeration
                    var dataList = healthChecksData.ToList();
                    
                    // Ensure CreationDate is set on all items
                    foreach (var data in dataList.Where(d => d.CreationDate == default))
                    {
                        data.CreationDate = DateTime.UtcNow;
                    }

                    // Process each item individually to check status changes
                    foreach (var data in dataList)
                    {
                        // Find the latest check for this name and machine
                        var latestCheck = _collection
                            .Find(hc => hc.Name == data.Name && hc.MachineName == data.MachineName)
                            .OrderByDescending(hc => hc.LastUpdated)
                            .FirstOrDefault();

                        // If the latest check exists and has the same status, just update LastUpdated
                        if (latestCheck != null && latestCheck.Status == data.Status)
                        {
                            latestCheck.LastUpdated = DateTime.UtcNow;
                            latestCheck.Duration = data.Duration;
                            latestCheck.Description = data.Description;
                            latestCheck.CheckError = data.CheckError;
                            
                            _collection.Update(latestCheck);
                        }
                        // Otherwise, insert a new record
                        else
                        {
                            _collection.Insert(data);
                        }
                    }
                }
                
                return;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // Other methods remain unchanged...
        public Task<List<HealthCheckData>> GetLatestHealthChecksAsync()
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public Task<List<HealthCheckData>> GetHealthChecksByDateRangeAsync(DateTime from, DateTime to)
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public Task<IEnumerable<IGrouping<(string Name, string MachineName), HealthCheckData>>> GetGroupedHealthChecksAsync()
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public Task<IEnumerable<HealthCheckData>> GetHealthChecksInTimeWindowAsync(DateTime startTime, DateTime endTime)
        {
            try
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
            catch (Exception ex)
            {
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _database?.Dispose();
            }
            catch (Exception ex)
            {
                // Log error if needed
            }
        }
    }
}
