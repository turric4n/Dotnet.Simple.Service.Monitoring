using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Simple.Service.Monitoring.Library.Options;

namespace Dotnet.Simple.Service.Monitoring.Library.Models
{
    public class ServiceHealthCheck
    {
        public ServiceType ServiceType { get; set; }
        public string EndpointOrHost { get; set; }
        public int Port { get; set; }
        public int MonitoringInterval { get; set; }
        public HealthCheckConditions HealthCheckConditions { get; set; }
        public AlertBehaviour AlertBehaviour { get; set; }
    }
}
