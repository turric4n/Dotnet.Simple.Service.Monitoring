using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Simple.Service.Monitoring.UI.Options;

namespace Simple.Service.Monitoring.UI.Models
{
    public class IndexModel : PageModel
    {
        private readonly IMonitoringDataService _monitoringService;
        private readonly IOptions<MonitoringUiOptions> _monitoringUiOptions;

        public IndexModel(IMonitoringDataService monitoringService, IOptions<MonitoringUiOptions> monitoringUiOptions)
        {
            _monitoringUiOptions = monitoringUiOptions;
            _monitoringService = monitoringService;
        }

        // Expose the latest report directly
        public HealthReport Report { get; private set; }

        // Convenience properties for the view
        public IEnumerable<HealthCheckData> HealthChecks => Report?.HealthChecks ?? [];
        public string OverallStatus => Report?.Status ?? "Unknown";
        public string LastUpdated => Report?.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss UTC") ?? "";

        public string TotalDuration => Report?.TotalDuration.ToString() ?? "0 ms";

        public string HeaderTitle => $"{_monitoringUiOptions.Value.CompanyName} Health Monitoring Dashboard";

        public string LogoUrl => _monitoringUiOptions.Value.HeaderLogoUrl ?? "/images/logo.png";

        // New: Last five health checks (most recent by LastUpdated)
        public IEnumerable<HealthCheckData> LastFiveHealthChecks =>
            HealthChecks.OrderByDescending(hc => hc.LastUpdated).Take(5);

        // New: Failed health checks timeline (status != Healthy, most recent first)
        public IEnumerable<HealthCheckData> FailedHealthChecksTimeline =>
            HealthChecks
                .Where(hc => hc.Status != HealthStatus.Healthy)
                .OrderByDescending(hc => hc.LastUpdated);

        public async Task OnGetAsync()
        {
            try
            {
                // Get the latest report (if any)
                Report = await _monitoringService.GetHealthCheckReport();
            }
            catch (Exception ex)
            {
                // Handle or log the exception
                // You might want to set a default/empty report here
                Report = new HealthReport
                {
                    Status = "Error",
                    LastUpdated = DateTime.UtcNow,
                    HealthChecks = new List<HealthCheckData>()
                };
            }
        }
    }
}