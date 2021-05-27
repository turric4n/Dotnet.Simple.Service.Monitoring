using System;
using System.Collections.Generic;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.InfluxDB;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations
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
            HttpServiceMonitoring mymonitor = null;

            switch (monitor.ServiceType)
            {
                case ServiceType.HttpEndpoint:
                    mymonitor = new HttpServiceMonitoring(_healthChecksBuilder, monitor);
                    break;
                case ServiceType.ElasticSearch:
                    break;
                case ServiceType.Sql:
                    break;
                case ServiceType.Rmq:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();

            }
            mymonitor?.SetUp();

            _monitors.Add(mymonitor);

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
                case InfluxDBTransportSettings _:
                {
                    publisher = new InfluxDBAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
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
            }

            publisher?.SetUp();

            return this;
        }

        public List<ServiceMonitoringBase> GetMonitors()
        {
            return _monitors;
        }


        public List<PublisherBase> GetPublishers()
        {
            return _publishers;
        }
    }
}
