using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Simple.Service.Monitoring.UI.Models;
using Simple.Service.Monitoring.UI.Services;
using System;
using System.Threading.Tasks;

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

        public Task<HealthReport> RetrieveHealthChecksReport()
        {
            var healthChecksReport = _monitoringDataService.GetHealthCheckReport();

            return Task.FromResult(healthChecksReport);
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
