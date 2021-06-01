using System;
using System.Collections.Generic;
using System.Text;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Extensions
{
    public interface IServiceMonitoringBuilder
    {
        IServiceMonitoringBuilder Add(ServiceHealthCheck monitor);
        IServiceMonitoringBuilder UseSettings();
        IServiceMonitoringBuilder AddObserver(IObserver<HealthReport> observer);
    }
}
