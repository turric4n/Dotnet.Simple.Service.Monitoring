using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Simple.Service.Monitoring.UI.Models;
using Simple.Service.Monitoring.UI.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.UI.Hubs
{
    public class MonitoringHub : Hub
    {
        private readonly ILogger<MonitoringHub> _logger;
        private readonly IMonitoringDataService _monitoringDataService;

        public MonitoringHub(ILogger<MonitoringHub> logger, IMonitoringDataService monitoringDataService)
        {
            _logger = logger;
            _monitoringDataService = monitoringDataService;
        }

        public async Task<HealthReport> RetrieveHealthChecksReport()
        {
            try
            {
                var healthChecksReport = await _monitoringDataService.GetHealthCheckReport();
                return healthChecksReport;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving health checks report");
                throw;
            }
        }

        public async Task RequestHealthChecksTimeline(int hours = 24)
        {
            await _monitoringDataService.SendHealthCheckTimeline(hours);
        }

        public async Task RequestHealthChecksTimelineGroupedByService(int hours = 24, bool activeOnly = false, int activeThresholdMinutes = 60)
        {
            await _monitoringDataService.SendHealthCheckTimelineGroupedByService(hours, activeOnly, activeThresholdMinutes);
        }

        public async Task SendHealthCheck(HealthCheckData healthCheckData)
        {
            await _monitoringDataService.AddHealthCheckData(healthCheckData);
        }

        public async Task SendHealthChecks(List<HealthCheckData> healthChecksData)
        {
            await _monitoringDataService.AddHealthChecksData(healthChecksData);
        }

        public override Task OnConnectedAsync()
        {
            _logger.LogInformation($"[Client Connected] > {Context.ConnectionId} - {Context?.User?.Identity?.Name}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"[Client Disconnected] > {Context.ConnectionId} - {Context?.User?.Identity?.Name}");
            return base.OnDisconnectedAsync(exception);
        }
    }
}
