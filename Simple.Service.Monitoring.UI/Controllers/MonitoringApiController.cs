using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Simple.Service.Monitoring.UI.Options;
using Simple.Service.Monitoring.UI.Services;
using System;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Controllers
{
    [ApiController]
    [Route("monitoring/api")]
    public class MonitoringApiController : ControllerBase
    {
        private readonly IMonitoringDataService _monitoringDataService;
        private readonly IOptions<MonitoringUiOptions> _options;

        public MonitoringApiController(
            IMonitoringDataService monitoringDataService,
            IOptions<MonitoringUiOptions> options)
        {
            _monitoringDataService = monitoringDataService;
            _options = options;
        }

        [HttpGet("health-report")]
        public async Task<IActionResult> GetHealthReport()
        {
            var report = await _monitoringDataService.GetHealthCheckReport();
            return Ok(report);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetOverallStatus()
        {
            var status = await _monitoringDataService.GetOverallStatus();
            return Ok(new { status = status.ToString() });
        }

        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline([FromQuery] int hours = 24)
        {
            if (hours < 1 || hours > 720) hours = 24;
            var timeline = await _monitoringDataService.GetHealthCheckTimeline(hours);
            return Ok(timeline);
        }

        [HttpGet("timeline/grouped")]
        public async Task<IActionResult> GetTimelineGrouped(
            [FromQuery] int hours = 24,
            [FromQuery] bool activeOnly = false,
            [FromQuery] int activeThresholdMinutes = 60)
        {
            if (hours < 1 || hours > 720) hours = 24;
            var timeline = await _monitoringDataService.GetHealthCheckTimelineGroupedByService(
                hours, activeOnly, activeThresholdMinutes);
            return Ok(timeline);
        }

        [HttpGet("health-report/range")]
        public async Task<IActionResult> GetHealthReportByRange(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            if (from >= to) return BadRequest("'from' must be before 'to'");
            var report = await _monitoringDataService.GetHealthReportByDateRange(from, to);
            return Ok(report);
        }

        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            var opts = _options.Value;
            return Ok(new
            {
                companyName = opts.CompanyName,
                headerLogoUrl = opts.HeaderLogoUrl
            });
        }
    }
}
