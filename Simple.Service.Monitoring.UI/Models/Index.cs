using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.UI.Services;
using System.Collections.Generic;
using System.Linq;

namespace Simple.Service.Monitoring.UI.Models
{
    public class IndexModel : PageModel
    {
        private readonly IMonitoringDataService _monitoringService;

        public IndexModel(IMonitoringDataService monitoringService)
        {
            _monitoringService = monitoringService;
        }

        // Expose the latest report directly
        public HealthReport Report { get; private set; }

        // Convenience properties for the view
        public IEnumerable<HealthCheckData> HealthChecks => Report?.HealthChecks ?? [];
        public string OverallStatus => Report?.Status ?? "Unknown";
        public string LastUpdated => Report?.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "";

        // New: Last five health checks (most recent by LastUpdated)
        public IEnumerable<HealthCheckData> LastFiveHealthChecks =>
            HealthChecks.OrderByDescending(hc => hc.LastUpdated).Take(5);

        // New: Failed health checks timeline (status != Healthy, most recent first)
        public IEnumerable<HealthCheckData> FailedHealthChecksTimeline =>
            HealthChecks
                .Where(hc => hc.Status != HealthStatus.Healthy)
                .OrderByDescending(hc => hc.LastUpdated);

        public void OnGet()
        {
            // Get the latest report (if any)
            Report = _monitoringService.GetHealthCheckReport();
        }
    }
}