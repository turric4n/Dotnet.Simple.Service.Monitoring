using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.UI.Services
{
    public class MonitoringDataService
    {
        private readonly ConcurrentDictionary<string, HealthCheckData> _healthChecks = new();

        public class HealthCheckData
        {
            public string Name { get; set; }
            public HealthReportEntry Entry { get; set; }
            public DateTime LastUpdated { get; set; }
            public HealthStatus Status => Entry.Status;
            public TimeSpan Duration => Entry.Duration;
            public string Description => Entry.Description;
        }

        public IEnumerable<HealthCheckData> GetAllHealthChecks() =>
            _healthChecks.Values.OrderBy(c => c.Name).ToList();

        public HealthCheckData GetHealthCheckByName(string name) =>
            _healthChecks.TryGetValue(name, out var data) ? data : null;

        public void AddHealthReport(HealthReport report)
        {
            foreach (var entry in report.Entries)
            {
                string healthCheckName = entry.Key;
                var healthCheckData = new HealthCheckData
                {
                    Name = healthCheckName,
                    Entry = entry.Value,
                    LastUpdated = DateTime.UtcNow
                };

                _healthChecks[healthCheckName] = healthCheckData;
            }
        }

        public HealthStatus GetOverallStatus()
        {
            var checks = _healthChecks.Values.ToList();
            if (!checks.Any())
                return HealthStatus.Unhealthy;

            if (checks.Any(c => c.Status == HealthStatus.Unhealthy))
                return HealthStatus.Unhealthy;

            if (checks.Any(c => c.Status == HealthStatus.Degraded))
                return HealthStatus.Degraded;

            return HealthStatus.Healthy;
        }
    }
}