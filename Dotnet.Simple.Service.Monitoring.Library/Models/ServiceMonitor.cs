using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dotnet.Simple.Service.Monitoring.Library.Models
{
    public class ServiceMonitor
    {
        public string Endpoint { get; set; }
        public ServiceType ServiceType { get; set; }
        public int MonitoringInterval { get; set; }
    }
}
