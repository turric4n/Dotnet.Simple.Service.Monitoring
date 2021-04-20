using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Microsoft.Extensions.Configuration;

namespace Dotnet.Simple.Service.Monitoring.Extensions
{
    public interface IServiceMonitoringBuilder
    {
        IServiceMonitoringBuilder Add(ServiceHealthCheck monitor);
        IServiceMonitoringBuilder UseSettings();
        IServiceMonitoringBuilder AddUI();
    }
}
