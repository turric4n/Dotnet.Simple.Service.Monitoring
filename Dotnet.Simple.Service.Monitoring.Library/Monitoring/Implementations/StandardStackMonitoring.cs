using System;
using System.Collections.Generic;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class StandardStackMonitoring : IStackMonitoring
    {
        private readonly IHealthChecksBuilder _healthChecksBuilder;
        private readonly List<PublisherBase> _publishers;

        public StandardStackMonitoring(IHealthChecksBuilder healthChecksBuilder)
        {
            _healthChecksBuilder = healthChecksBuilder;
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
            return this;
        }

        public IStackMonitoring AddPublishing(AlertTransportSettings alertTransportSettings, ServiceHealthCheck monitor)
        {
            PublisherBase publisher = null;

            if (alertTransportSettings is EmailTransportSettings)
            {
                publisher = new EmailAlertingPublisher(_healthChecksBuilder, monitor, alertTransportSettings);
                lock (_publishers)
                {
                    _publishers.Add(publisher);
                }
            }

            publisher?.SetUp();

            return this;
        }

        public List<PublisherBase> GetPublishers()
        {
            return _publishers;
        }
    }
}
