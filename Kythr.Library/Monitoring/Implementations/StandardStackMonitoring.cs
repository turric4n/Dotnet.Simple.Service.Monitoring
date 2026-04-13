using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Implementations.Publishers.AppInsights;
using Kythr.Library.Monitoring.Implementations.Publishers.CloudWatch;
using Kythr.Library.Monitoring.Implementations.Publishers.Console;
using Kythr.Library.Monitoring.Implementations.Publishers.CustomNotificationService;
using Kythr.Library.Monitoring.Implementations.Publishers.Datadog;
using Kythr.Library.Monitoring.Implementations.Publishers.Discord;
using Kythr.Library.Monitoring.Implementations.Publishers.Elasticsearch;
using Kythr.Library.Monitoring.Implementations.Publishers.Email;
using Kythr.Library.Monitoring.Implementations.Publishers.FileLog;
using Kythr.Library.Monitoring.Implementations.Publishers.GoogleChat;
using Kythr.Library.Monitoring.Implementations.Publishers.InfluxDB;
using Kythr.Library.Monitoring.Implementations.Publishers.KafkaPublisher;
using Kythr.Library.Monitoring.Implementations.Publishers.Mattermost;
using Kythr.Library.Monitoring.Implementations.Publishers.Opsgenie;
using Kythr.Library.Monitoring.Implementations.Publishers.PagerDuty;
using Kythr.Library.Monitoring.Implementations.Publishers.Prometheus;
using Kythr.Library.Monitoring.Implementations.Publishers.RabbitMQ;
using Kythr.Library.Monitoring.Implementations.Publishers.RedisPublisher;
using Kythr.Library.Monitoring.Implementations.Publishers.SignalRPublisher;
using Kythr.Library.Monitoring.Implementations.Publishers.Slack;
using Kythr.Library.Monitoring.Implementations.Publishers.Teams;
using Kythr.Library.Monitoring.Implementations.Publishers.Telegram;
using Kythr.Library.Monitoring.Implementations.Publishers.WebhookPublisher;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kythr.Library.Monitoring.Implementations
{
    public class StandardStackMonitoring : IStackMonitoring
    {
        private readonly IHealthChecksBuilder _healthChecksBuilder;
        private readonly List<PublisherBase> _publishers;
        private readonly List<ServiceMonitoringBase> _monitors;

        public StandardStackMonitoring(IHealthChecksBuilder healthChecksBuilder)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _monitors = new List<ServiceMonitoringBase>();
            _publishers = new List<PublisherBase>();
        }
        public IStackMonitoring AddMonitoring(ServiceHealthCheck monitor)
        {
            ServiceMonitoringBase mymonitor = null;

            switch (monitor.ServiceType)
            {

                case ServiceType.Http:
                    mymonitor = new HttpServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.ElasticSearch:
                    mymonitor = new ElasticSearchServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.MsSql:
                    mymonitor = new MsSqlServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Rmq:
                    mymonitor = new RmqServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Hangfire:
                    mymonitor = new HangfireServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Custom:
                    mymonitor = new CustomMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Ping:
                    mymonitor = new PingServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Redis:
                    mymonitor = new RedisServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.MySql:
                    mymonitor = new MySqlServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.PostgreSql:
                    mymonitor = new PostgreSqlServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Interceptor:
                    mymonitor = new InterceptionMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.MongoDb:
                    mymonitor = new MongoDbServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.CosmosDb:
                    mymonitor = new CosmosDbServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Kafka:
                    mymonitor = new KafkaServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Grpc:
                    mymonitor = new GrpcServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Tcp:
                    mymonitor = new TcpServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Dns:
                    mymonitor = new DnsServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.SslCertificate:
                    mymonitor = new SslCertificateServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Ftp:
                    mymonitor = new FtpServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Smtp:
                    mymonitor = new SmtpServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.AzureServiceBus:
                    mymonitor = new AzureServiceBusServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Memcached:
                    mymonitor = new MemcachedServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Oracle:
                    mymonitor = new OracleServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Sqlite:
                    mymonitor = new SqliteServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.Docker:
                    mymonitor = new DockerServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.AwsSqs:
                    mymonitor = new AwsSqsServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }

            mymonitor?.SetUp();

            _monitors.Add(mymonitor);

            return this;
        }
        public IStackMonitoring AddCustomHealthCheck(IHealthCheck healthCheck, string name, IEnumerable<string> tags)
        {
            _healthChecksBuilder.AddCheck(name, healthCheck, null, tags);
            return this;
        }

        public IStackMonitoring AddPublishing(AlertTransportSettings alertTransportSettings, ServiceHealthCheck monitor)
        {
            PublisherBase publisher = null;

            switch (alertTransportSettings)
            {
                case EmailTransportSettings _:
                    {
                        publisher = new EmailAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers)
                        {
                            _publishers.Add(publisher);
                        }

                        break;
                    }
                case InfluxDbTransportSettings _:
                    {
                        publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers)
                        {
                            _publishers.Add(publisher);
                        }

                        break;
                    }

                case CustomNotificationTransportSettings _:
                    {
                        publisher = new CustomNotificationAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers)
                        {
                            _publishers.Add(publisher);
                        }

                        break;
                    }

                case TelegramTransportSettings _:
                    {
                        publisher = new TelegramAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers)
                        {
                            _publishers.Add(publisher);
                        }

                        break;
                    }

                case SlackTransportSettings _:
                    {
                        publisher = new SlackAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers)
                        {
                            _publishers.Add(publisher);
                        }

                        break;
                    }

                case SignalRTransportSettings _:
                    {

                        publisher = new SignalRAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers)
                        {
                            _publishers.Add(publisher);
                        }
                        break;
                    }

                case TeamsTransportSettings _:
                    {
                        publisher = new TeamsAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case DiscordTransportSettings _:
                    {
                        publisher = new DiscordAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case PagerDutyTransportSettings _:
                    {
                        publisher = new PagerDutyAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case OpsgenieTransportSettings _:
                    {
                        publisher = new OpsgenieAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case DatadogTransportSettings _:
                    {
                        publisher = new DatadogAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case PrometheusTransportSettings _:
                    {
                        publisher = new PrometheusAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case CloudWatchTransportSettings _:
                    {
                        publisher = new CloudWatchAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case AppInsightsTransportSettings _:
                    {
                        publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case ElasticsearchTransportSettings _:
                    {
                        publisher = new ElasticsearchAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case GoogleChatTransportSettings _:
                    {
                        publisher = new GoogleChatAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case MattermostTransportSettings _:
                    {
                        publisher = new MattermostAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case ConsoleTransportSettings _:
                    {
                        publisher = new ConsoleAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case FileTransportSettings _:
                    {
                        publisher = new FileAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case RmqTransportSettings _:
                    {
                        publisher = new RmqAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case KafkaTransportSettings _:
                    {
                        publisher = new KafkaAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case WebhookTransportSettings _:
                    {
                        publisher = new WebhookAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }

                case RedisTransportSettings _:
                    {
                        publisher = new RedisAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                        lock (_publishers) { _publishers.Add(publisher); }
                        break;
                    }
            }


            publisher?.SetUp();

            return this;
        }

        public IStackMonitoring AddCustomPublisher(PublisherBase publisher)
        {
            lock (_publishers)
            {
                _publishers?.Add(publisher);
                publisher.SetUp();
            }

            return this;
        }

        public List<ServiceMonitoringBase> GetMonitors()
        {
            return _monitors;
        }

        public CustomMonitoring GetCustomMonitor(string name)
        {
            return _monitors.FirstOrDefault(x => x.GetType() == typeof(CustomMonitoring) && x.Name == name) as CustomMonitoring;
        }


        public List<PublisherBase> GetPublishers()
        {
            lock (_publishers)
            {
                return _publishers;
            }
        }
    }
}
