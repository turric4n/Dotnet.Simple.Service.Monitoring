using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.UI.Services
{
    public class MonitoringDataService
    {
        private readonly ConcurrentBag<HealthReport> _healthReports = new();

        public IEnumerable<HealthReport> GetHealthReports() => _healthReports.ToList();

        public void AddHealthReport(HealthReport report)
        {
            _healthReports.Add(report);
        }
    }
}