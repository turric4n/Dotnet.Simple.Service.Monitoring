using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring
{
    public interface IStackMonitoring
    {
        void AddMonitoring(ServiceHealthCheck monitor);
    }
}
