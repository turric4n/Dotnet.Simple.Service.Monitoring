using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using Microsoft.Extensions.Logging;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.UI.Repositories.Memory
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
                
                //_logger.LogInformation("LiteDB repository initialized at {DbPath}", dbPath);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Failed to initialize LiteDB repository at {DbPath}", dbPath);
                throw;
            }
        }

        public Task AddHealthCheckDataAsync(HealthCheckData healthCheckData)
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
                    
                    // Insert the document
                    _collection.Insert(healthCheckData);
                }
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error adding health check data for {Name}", healthCheckData.Name);
                throw;
            }
        }

        public Task AddHealthChecksDataAsync(IEnumerable<HealthCheckData> healthChecksData)
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
                    
                    // Insert all documents
                    _collection.InsertBulk(dataList);
                }
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error adding health checks data in bulk");
                throw;
            }
        }

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
                //_logger.LogError(ex, "Error retrieving latest health checks");
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
                //_logger.LogError(ex, "Error retrieving health checks by date range: {From} to {To}", from, to);
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
                //_logger.LogError(ex, "Error retrieving grouped health checks");
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
                //_logger.LogError(ex, "Error retrieving health checks in time window: {Start} to {End}", startTime, endTime);
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
                //_logger.LogError(ex, "Error disposing LiteDB database");
            }
        }
    }
}
