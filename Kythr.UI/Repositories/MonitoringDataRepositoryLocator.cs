using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Kythr.UI.Options;
using System;

namespace Kythr.UI.Repositories
{
    public class MonitoringDataRepositoryLocator : IMonitoringDataRepositoryLocator
    {
        private readonly IOptions<MonitoringUiOptions> _monitoringUiOptions;
        private readonly IServiceProvider _serviceProvider;

        public MonitoringDataRepositoryLocator(IOptions<MonitoringUiOptions> monitoringUiOptions, IServiceProvider serviceProvider)
        {
            _monitoringUiOptions = monitoringUiOptions;
            _serviceProvider = serviceProvider;
        }

        public IMonitoringDataRepository GetMonitoringDataRepository()
        {
            var repositoryType = _monitoringUiOptions.Value.DataRepositoryType;
            return repositoryType switch
            {
                DataRepositoryType.InMemory => 
                    _serviceProvider.GetRequiredKeyedService<IMonitoringDataRepository>("InMemory"),
                DataRepositoryType.LiteDb => 
                    _serviceProvider.GetRequiredKeyedService<IMonitoringDataRepository>("LiteDb"),
                DataRepositoryType.Sql => 
                    _serviceProvider.GetRequiredKeyedService<IMonitoringDataRepository>("Sql"),
                _ => 
                    throw new NotSupportedException($"Repository type '{repositoryType}' is not supported.")
            };
        }
    }
}
