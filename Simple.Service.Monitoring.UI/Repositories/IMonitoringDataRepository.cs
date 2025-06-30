using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Repositories
{
    public interface IMonitoringDataRepository
    {
        /// <summary>
        /// Adds a single health check data entry
        /// </summary>
        Task AddHealthCheckDataAsync(HealthCheckData healthCheckData);

        /// <summary>
        /// Adds multiple health check data entries
        /// </summary>
        Task AddHealthChecksDataAsync(IEnumerable<HealthCheckData> healthChecksData);

        /// <summary>
        /// Gets the latest health check data for each unique name and machine name combination
        /// </summary>
        Task<List<HealthCheckData>> GetLatestHealthChecksAsync();

        /// <summary>
        /// Gets health check data within a specific date range
        /// </summary>
        Task<List<HealthCheckData>> GetHealthChecksByDateRangeAsync(DateTime from, DateTime to);

        /// <summary>
        /// Gets all health check data grouped by name and machine name
        /// </summary>
        Task<IEnumerable<IGrouping<(string Name, string MachineName), HealthCheckData>>> GetGroupedHealthChecksAsync();

        /// <summary>
        /// Gets all health check data within a specific time window
        /// </summary>
        Task<IEnumerable<HealthCheckData>> GetHealthChecksInTimeWindowAsync(DateTime startTime, DateTime endTime);
    }
}