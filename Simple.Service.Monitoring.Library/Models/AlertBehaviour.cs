using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Simple.Service.Monitoring.Library.Models
{
    public class AlertBehaviour
    {
        public AlertTransportMethod TransportMethod { get; set; }
        public string TransportName { get; set; }
        public bool AlertOnce { get; set; }
        public bool AlertOnServiceRecovered { get; set; }
        public TimeSpan AlertEvery { get; set; }
        public TimeSpan StartAlertingOn { get; set; } = TimeSpan.Parse("00:00:00");
        public TimeSpan StopAlertingOn { get; set; } = TimeSpan.Parse("23:59:59");
        public int AlertByFailCount { get; set; }
        public DateTime LastCheck { get; set; }
        public DateTime LastPublished { get; set; }
        public bool LatestErrorPublished { get; set; }
        public HealthStatus LastStatus { get; set; }
        public int FailedCount { get; set; }
        public bool PublishAllResults { get; set; }
        public bool IncludeEnvironment { get; set; }
        public string Timezone { get; set; }
    }
}
