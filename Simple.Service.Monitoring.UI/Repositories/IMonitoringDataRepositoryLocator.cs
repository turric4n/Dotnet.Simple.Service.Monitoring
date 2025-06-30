using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.UI.Repositories
{
    public interface IMonitoringDataRepositoryLocator
    {
        /// <summary>
        /// Gets the repository for monitoring data.
        /// </summary>
        /// <returns>An instance of IMonitoringDataRepository.</returns>
        IMonitoringDataRepository GetMonitoringDataRepository();
    }
}
