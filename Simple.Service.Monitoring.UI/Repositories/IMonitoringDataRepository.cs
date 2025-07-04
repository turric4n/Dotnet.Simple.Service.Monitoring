using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.UI.Models;
using HealthStatus = Simple.Service.Monitoring.Library.Models.HealthStatus;

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
        /// Gets the latest health check data for a specific service identified by name and machine name
        /// </summary>
        Task<HealthCheckData> GetLatestHealthCheckAsync(string name, string machineName);

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

        /// <summary>
        /// Adds a single time series point
        /// </summary>
        Task AddTimeSeriesPointAsync(HealthCheckTimeSeriesPoint point);

        /// <summary>
        /// Adds multiple time series points
        /// </summary>
        Task AddTimeSeriesPointsAsync(IEnumerable<HealthCheckTimeSeriesPoint> points);

        /// <summary>
        /// Gets time series points within a specific time window
        /// </summary>
        Task<List<HealthCheckTimeSeriesPoint>> GetTimeSeriesPointsAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Gets grouped time series points within a specific time window
        /// </summary>
        Task<Dictionary<string, List<HealthCheckTimeSeriesPoint>>> GetGroupedTimeSeriesPointsAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Adds a health check status change
        /// </summary>
        Task AddHealthCheckStatusChangeAsync(string name, string machineName, DateTime timestamp, HealthStatus status, string statusReason);

        /// <summary>
        /// Gets health check time ranges within a specific time window
        /// </summary>
        Task<List<HealthCheckTimeRange>> GetHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Gets grouped health check time ranges within a specific time window
        /// </summary>
        Task<Dictionary<string, List<HealthCheckTimeRange>>> GetGroupedHealthCheckTimeRangesAsync(DateTime startTime, DateTime endTime);
    }
}