using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring
{
    public class StandardStackMonitoring : IStackMonitoring
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly IHealthChecksBuilder _healthChecksBuilder;

        public StandardStackMonitoring(IHealthChecksBuilder healthChecksBuilder)
        {
            _healthChecksBuilder = healthChecksBuilder;
        }
        public void AddMonitoring(ServiceHealthCheck monitor)
        {
            
        }
    }
}
