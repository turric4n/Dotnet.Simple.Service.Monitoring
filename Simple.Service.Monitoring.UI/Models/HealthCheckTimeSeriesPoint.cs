using System;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.UI.Models
{
    public class HealthCheckTimeSeriesPoint
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string MachineName { get; set; }
        public DateTime Timestamp { get; set; }
        public HealthStatus Status { get; set; }
        public string StatusReason { get; set; }
        
        // Composite key for easy grouping
        public string ServiceKey => string.IsNullOrEmpty(MachineName) 
            ? Name 
            : $"{Name} ({MachineName})";
    }
}