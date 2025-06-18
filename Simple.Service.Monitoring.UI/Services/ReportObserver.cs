using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;

namespace Simple.Service.Monitoring.UI.Services
{
    public class ReportObserver : IReportObserver
    {
        private readonly MonitoringDataService _dataService;

        public ReportObserver(MonitoringDataService dataService, bool executeAlways)
        {
            _dataService = dataService;
            ExecuteAlways = executeAlways;
        }

        public bool ExecuteAlways { get; }

        public void OnCompleted()
        {
            // Do nothing
        }

        public void OnError(Exception error)
        {
            // Log error
        }

        public void OnNext(HealthReport value)
        {
            _dataService
                .AddHealthReport(value)
                .ConfigureAwait(false);
        }
    }
}