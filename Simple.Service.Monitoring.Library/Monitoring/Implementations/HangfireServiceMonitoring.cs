using CuttingEdge.Conditions;
using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IHealthChecksBuilder = Microsoft.Extensions.DependencyInjection.IHealthChecksBuilder;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations
{
    public class HangfireServiceMonitoring : ServiceMonitoringBase
    {

        public HangfireServiceMonitoring(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck) : base(healthChecksBuilder, healthCheck)
        {
        }

        protected internal override void Validate()
        {
            var csbuilder = new DbConnectionStringBuilder();
            Condition.Requires(this.HealthCheck.HealthCheckConditions)
                .IsNotNull();
            Condition.Requires(this.HealthCheck.ConnectionString)
                .IsNotNull();
            Condition
                .Ensures(csbuilder.ConnectionString = this.HealthCheck.ConnectionString);
        }

        protected internal override void SetMonitoring()
        {
            var minimumAvailableServers =
                this.HealthCheck.HealthCheckConditions.HangfireBehaviour?.MinimumAvailableServers;

            var maximumJobsFailed =
                this.HealthCheck.HealthCheckConditions.HangfireBehaviour?.MaximumJobsFailed;

            HealthChecksBuilder.AddCheck(
                HealthCheck.Name,
                new HangfireHealthCheck(minimumAvailableServers, maximumJobsFailed),
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
                GetTags());
        }

    }

    /// <summary>
    /// Custom Hangfire health check implementation
    /// </summary>
    internal class HangfireHealthCheck : IHealthCheck
    {
        private readonly int? _minimumAvailableServers;
        private readonly int? _maximumJobsFailed;

        public HangfireHealthCheck(int? minimumAvailableServers, int? maximumJobsFailed)
        {
            _minimumAvailableServers = minimumAvailableServers;
            _maximumJobsFailed = maximumJobsFailed;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var storage = JobStorage.Current;
                
                using var connection = storage.GetConnection();
                var monitoringApi = storage.GetMonitoringApi();

                var servers = monitoringApi.Servers();
                var serverCount = servers.Count;

                if (_minimumAvailableServers.HasValue && serverCount < _minimumAvailableServers.Value)
                {
                    return Task.FromResult(
                        HealthCheckResult.Unhealthy($"Hangfire has {serverCount} servers, minimum required is {_minimumAvailableServers.Value}")
                    );
                }

                var failedCount = monitoringApi.FailedCount();

                if (_maximumJobsFailed.HasValue && failedCount > _maximumJobsFailed.Value)
                {
                    return Task.FromResult(
                        HealthCheckResult.Unhealthy($"Hangfire has {failedCount} failed jobs, maximum allowed is {_maximumJobsFailed.Value}")
                    );
                }

                return Task.FromResult(
                    HealthCheckResult.Healthy($"Hangfire is healthy with {serverCount} servers and {failedCount} failed jobs")
                );
            }
            catch (Exception ex)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy($"Hangfire health check failed: {ex.Message}", ex)
                );
            }
        }
    }
}
