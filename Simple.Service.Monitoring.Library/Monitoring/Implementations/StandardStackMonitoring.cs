using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.CustomNotificationService;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.InfluxDB;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.SignalRPublisher;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
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
