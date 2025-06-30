using Microsoft.Extensions.Options;
using Simple.Service.Monitoring.UI.Repositories.Memory;
using Simple.Service.Monitoring.UI.Settings;
using System;

namespace Simple.Service.Monitoring.UI.Repositories
{
    public class MonitoringDataRepositoryLocator : IMonitoringDataRepositoryLocator
    {
        private readonly IOptions<MonitoringUiSettings> _monitoringUiOptions;

        public MonitoringDataRepositoryLocator(IOptions<MonitoringUiSettings> monitoringUiOptions)
        {
            _monitoringUiOptions = monitoringUiOptions;
        }

        public IMonitoringDataRepository GetMonitoringDataRepository()
        {
            var repositoryType = _monitoringUiOptions.Value.DataRepositoryType;
            return repositoryType switch
            {
                DataRepositoryType.InMemory => new InMemoryMonitoringDataRepository(),
                DataRepositoryType.LiteDb => new LiteDbMonitoringDatarepository(),
                _ => throw new NotSupportedException($"Repository type '{repositoryType}' is not supported.")
            };
        }
    }
}
