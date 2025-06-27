using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers
{
    public class DictionaryPublisher : PublisherBase
    {
        private ConcurrentDictionary<string, ConcurrentDictionary<DateTime, HealthReport>> _reportDictionary;

        public DictionaryPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _reportDictionary = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, HealthReport>>();
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);

            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                StoreHealthReportForEntry(ownedEntry.Key, report);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    StoreHealthReportForEntry(interceptedEntry.Key, report);
                }
            }

            return Task.CompletedTask;
        }

        private void StoreHealthReportForEntry(string healthCheckName, HealthReport report)
        {
            // Get or create dictionary for this health check
            var reportsByTime = _reportDictionary.GetOrAdd(
                healthCheckName, 
                _ => new ConcurrentDictionary<DateTime, HealthReport>()
            );

            // Add the report with current timestamp
            reportsByTime.AddOrUpdate(DateTime.UtcNow, report, (time, oldReport) => report);

            // Optional: Cleanup old entries to prevent memory leaks
            CleanupOldEntries(reportsByTime);
        }

        private void CleanupOldEntries(ConcurrentDictionary<DateTime, HealthReport> reports)
        {
            // Remove entries older than 24 hours to prevent unlimited growth
            var cutoffTime = DateTime.UtcNow.AddHours(-24);

            foreach (var key in reports.Keys)
            {
                if (key < cutoffTime)
                {
                    reports.TryRemove(key, out _);
                }
            }
        }

        // Method to retrieve reports for a specific health check
        public ConcurrentDictionary<DateTime, HealthReport> GetReportsForHealthCheck(string healthCheckName)
        {
            return _reportDictionary.TryGetValue(healthCheckName, out var reports) 
                ? reports 
                : new ConcurrentDictionary<DateTime, HealthReport>();
        }

        protected internal override void Validate()
        {
            // No validation needed for dictionary publisher
            return;
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });
        }
    }
}
