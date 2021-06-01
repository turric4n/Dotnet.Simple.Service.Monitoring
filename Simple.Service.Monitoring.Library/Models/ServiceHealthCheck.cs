using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Models
{
    public class ServiceHealthCheck
    {
        public string Name { get; set; }
        public ServiceType ServiceType { get; set; }
        public string EndpointOrHost { get; set; }
        public int Port { get; set; }
        public TimeSpan MonitoringInterval { get; set; }
        public HealthCheckConditions HealthCheckConditions { get; set; }
        public bool PublishChecks { get; set; }
        public bool Alert { get; set; }
        public List<AlertBehaviour> AlertBehaviour { get; set; }
    }
}
