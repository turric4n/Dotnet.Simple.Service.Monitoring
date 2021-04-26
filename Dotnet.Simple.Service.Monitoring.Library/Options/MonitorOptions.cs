using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;

namespace Dotnet.Simple.Service.Monitoring.Library.Options
{
    public class MonitorOptions
    {
        public MonitoringSettings Settings { get; set; }
        public List<ServiceHealthCheck> HealthChecks { get; set; }
        public List<EmailTransportSettings> EmailTransportSettings { get; set; }
        public List<SlackTransportSettings> SlackTransportSettings { get; set; }
        public List<TelegramTransportSettings> TelegramTransportSettings { get; set; }
    }
}
