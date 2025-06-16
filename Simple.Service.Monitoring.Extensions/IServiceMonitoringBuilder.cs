using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;

namespace Simple.Service.Monitoring.Extensions
{
    public interface IServiceMonitoringBuilder
    {
        IServiceMonitoringBuilder Add(ServiceHealthCheck monitor);
        IServiceMonitoringBuilder UseSettings();
        IServiceMonitoringBuilder AddPublisherObserver(IObserver<HealthReport> observer);
        IServiceMonitoringBuilder AddPublishing(ServiceHealthCheck monitor);
    }
}
