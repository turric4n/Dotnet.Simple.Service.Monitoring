using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kythr.Library.Monitoring.Implementations.Publishers.Console
{
    public class ConsoleAlertingPublisher : PublisherBase
    {
        private readonly ConsoleTransportSettings _consoleTransportSettings;

        public ConsoleAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _consoleTransportSettings = (ConsoleTransportSettings)alertTransportSettings;
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                WriteToConsole(ownedEntry);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    WriteToConsole(interceptedEntry);
                }
            }

            return Task.CompletedTask;
        }

        private void WriteToConsole(KeyValuePair<string, HealthReportEntry> entry)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var useColors = _consoleTransportSettings.UseColors;
                var format = _consoleTransportSettings.OutputFormat ?? "text";

                if (useColors)
                {
                    var originalColor = System.Console.ForegroundColor;
                    System.Console.ForegroundColor = GetConsoleColor(healthCheckData.Status);

                    WriteFormattedOutput(healthCheckData, format);

                    System.Console.ForegroundColor = originalColor;
                }
                else
                {
                    WriteFormattedOutput(healthCheckData, format);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to write console alert");
            }
        }

        private static void WriteFormattedOutput(HealthCheckData data, string format)
        {
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    timestamp = DateTime.Now,
                    name = data.Name,
                    status = data.Status.ToString(),
                    serviceType = data.ServiceType.ToString(),
                    durationMs = data.Duration,
                    machineName = data.MachineName,
                    description = data.Description
                }, Newtonsoft.Json.Formatting.None);
                System.Console.WriteLine(json);
            }
            else
            {
                var statusIcon = data.Status switch
                {
                    Models.HealthStatus.Unhealthy => "[FAIL]",
                    Models.HealthStatus.Degraded => "[WARN]",
                    Models.HealthStatus.Healthy => "[ OK ]",
                    _ => "[????]"
                };

                System.Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {statusIcon} {data.Name} | {data.ServiceType} | {data.Duration}ms | {data.Description}");
            }
        }

        private static ConsoleColor GetConsoleColor(Models.HealthStatus status)
        {
            return status switch
            {
                Models.HealthStatus.Unhealthy => ConsoleColor.Red,
                Models.HealthStatus.Degraded => ConsoleColor.Yellow,
                Models.HealthStatus.Healthy => ConsoleColor.Green,
                _ => ConsoleColor.Gray
            };
        }

        protected internal override void Validate()
        {
            // Console publisher has no required settings
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
