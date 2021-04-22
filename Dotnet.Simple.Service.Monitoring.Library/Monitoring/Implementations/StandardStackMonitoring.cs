using System;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class StandardStackMonitoring : IStackMonitoring
    {
        private readonly IHealthChecksBuilder _healthChecksBuilder;

        public StandardStackMonitoring(IHealthChecksBuilder healthChecksBuilder)
        {
            _healthChecksBuilder = healthChecksBuilder;
        }
        public void AddMonitoring(ServiceHealthCheck monitor)
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
        }
    }
}
