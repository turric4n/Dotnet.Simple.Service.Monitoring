using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;

namespace Simple.Service.Monitoring.Library.Options
{
    public class MonitorOptions
    {
        public MonitoringSettings Settings { get; set; }
        public List<ServiceHealthCheck> HealthChecks { get; set; }
        public List<EmailTransportSettings> EmailTransportSettings { get; set; }
        public List<SlackTransportSettings> SlackTransportSettings { get; set; }
        public List<TelegramTransportSettings> TelegramTransportSettings { get; set; }
        public List<InfluxDbTransportSettings> InfluxDbTransportSettings { get; set; }
        public List<CustomNotificationTransportSettings> CustomNotificationTransportSettings { get; set; }
    }
}
