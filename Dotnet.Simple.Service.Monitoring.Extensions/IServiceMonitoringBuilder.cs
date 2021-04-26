using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Extensions
{
    public interface IServiceMonitoringBuilder
    {
        IServiceMonitoringBuilder Add(ServiceHealthCheck monitor);
        IServiceMonitoringBuilder UseSettings();
        IServiceMonitoringBuilder AddObserver(IObserver<HealthReport> observer);
        IServiceMonitoringBuilder AddUI();
    }
}
