using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Options;
using System;
using System.Collections.Generic;

namespace Kythr.Extensions
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
