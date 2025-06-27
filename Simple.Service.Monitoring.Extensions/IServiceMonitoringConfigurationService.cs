using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using Simple.Service.Monitoring.Library.Options;
using System;
using System.Collections.Generic;

namespace Simple.Service.Monitoring.Extensions
{
    public interface IServiceMonitoringConfigurationService
    {
        IServiceMonitoringConfigurationService WithAdditionalCheck(ServiceHealthCheck monitor);
        IServiceMonitoringConfigurationService WithApplicationSettings();
        IServiceMonitoringConfigurationService WithRuntimeSettings(MonitorOptions options);
        IServiceMonitoringConfigurationService WithAdditionalPublisherObserver(IObserver<KeyValuePair<string, HealthReportEntry>> observer, bool useAlertingRules = true);
        IServiceMonitoringConfigurationService WithAdditionalPublishing(ServiceHealthCheck monitor);
        IServiceMonitoringConfigurationService WithAdditionalPublisher(PublisherBase publisher);
    }
}
