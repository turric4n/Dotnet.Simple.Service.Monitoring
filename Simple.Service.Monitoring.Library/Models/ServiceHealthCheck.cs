using System;
using System.Collections.Generic;

namespace Simple.Service.Monitoring.Library.Models
{
    public class ServiceHealthCheck
    {
        public ServiceHealthCheck()
        {
            HealthCheckConditions = new HealthCheckConditions();
            AlertBehaviour = new List<AlertBehaviour>();
        }

        public string Name { get; set; }
        public ServiceType ServiceType { get; set; }
        public string EndpointOrHost { get; set; }
        public string ConnectionString { get; set; }
        public TimeSpan MonitoringInterval { get; set; }
        public HealthCheckConditions HealthCheckConditions { get; set; }
        public bool Alert { get; set; }
        public List<AlertBehaviour> AlertBehaviour { get; set; }
        public string FullClassName { get; set; }
    }
}
