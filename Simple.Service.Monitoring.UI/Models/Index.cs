using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.UI.Services;

namespace Simple.Service.Monitoring.UI.Models
{
    public class IndexModel : PageModel
    {
        private readonly MonitoringDataService _monitoringService;

        public IndexModel(MonitoringDataService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        public IEnumerable<HealthReport> HealthReports { get; private set; }

        public void OnGet()
        {
            HealthReports = _monitoringService.GetHealthReports();
        }
    }
}