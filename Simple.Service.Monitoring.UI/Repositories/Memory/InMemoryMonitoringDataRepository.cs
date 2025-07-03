using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.UI.Repositories.Memory
{
    public class InMemoryMonitoringDataRepository : IMonitoringDataRepository
    {
        private readonly List<HealthCheckData> _healthCheckStore = new();
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
                    latestCheck.LastUpdated = DateTime.UtcNow;
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
                
                foreach (var data in healthChecksData)
                {
                    // Find the latest check for this name and machine
                    var latestCheck = _healthCheckStore
                        .Where(hc => hc.Name == data.Name && hc.MachineName == data.MachineName)
                        .OrderByDescending(hc => hc.LastUpdated)
                        .FirstOrDefault();

                    // If the latest check exists and has the same status, just update LastUpdated
                    if (latestCheck != null && latestCheck.Status == data.Status)
                    {
                        latestCheck.LastUpdated = DateTime.UtcNow;
                        latestCheck.Duration = data.Duration;
                        latestCheck.Description = data.Description;
                        latestCheck.CheckError = data.CheckError;
                    }
                    // Otherwise, add a new record
                    else
                    {
                        _healthCheckStore.Add(data);
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
    }
}
