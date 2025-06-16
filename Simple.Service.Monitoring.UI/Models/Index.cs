using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.UI.Services;
using System.Collections.Generic;
using static Simple.Service.Monitoring.UI.Services.MonitoringDataService;

namespace Simple.Service.Monitoring.UI.Models
{
    public class IndexModel : PageModel
    {
        private readonly MonitoringDataService _monitoringService;

        public IndexModel(MonitoringDataService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        public IEnumerable<HealthCheckData> HealthChecks { get; private set; }

        public HealthStatus GetOverallStatus() => _monitoringService.GetOverallStatus();

        public void OnGet()
        {
            HealthChecks = _monitoringService.GetAllHealthChecks();
        }
    }
}