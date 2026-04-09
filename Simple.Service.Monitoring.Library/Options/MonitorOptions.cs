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
        public List<SignalRTransportSettings> SignalRTransportSettings { get; set; }
        public List<TeamsTransportSettings> TeamsTransportSettings { get; set; }
        public List<DiscordTransportSettings> DiscordTransportSettings { get; set; }
        public List<PagerDutyTransportSettings> PagerDutyTransportSettings { get; set; }
        public List<OpsgenieTransportSettings> OpsgenieTransportSettings { get; set; }
        public List<DatadogTransportSettings> DatadogTransportSettings { get; set; }
        public List<PrometheusTransportSettings> PrometheusTransportSettings { get; set; }
        public List<CloudWatchTransportSettings> CloudWatchTransportSettings { get; set; }
        public List<AppInsightsTransportSettings> AppInsightsTransportSettings { get; set; }
        public List<ElasticsearchTransportSettings> ElasticsearchTransportSettings { get; set; }
        public List<GoogleChatTransportSettings> GoogleChatTransportSettings { get; set; }
        public List<MattermostTransportSettings> MattermostTransportSettings { get; set; }
        public List<ConsoleTransportSettings> ConsoleTransportSettings { get; set; }
        public List<FileTransportSettings> FileTransportSettings { get; set; }
        public List<RmqTransportSettings> RmqTransportSettings { get; set; }
        public List<KafkaTransportSettings> KafkaTransportSettings { get; set; }
        public List<WebhookTransportSettings> WebhookTransportSettings { get; set; }
        public List<RedisTransportSettings> RedisTransportSettings { get; set; }
    }
}
