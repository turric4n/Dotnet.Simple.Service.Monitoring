using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Simple.Service.Monitoring.Library.Models;

namespace Dotnet.Simple.Service.Monitoring.Library.Options
{
    public class MonitorOptions
    {
        public MonitoringSettings Settings { get; set; }
        public List<ServiceMonitor> Monitors { get; set; }
    }
}
