using System;
using System.Collections.Generic;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.UI.Models
{
    public class HealthReport
    {
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<HealthCheckData> HealthChecks { get; set; }
    }
}
