using CuttingEdge.Conditions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kythr.Library.Monitoring.Implementations.Publishers.FileLog
{
    public class FileAlertingPublisher : PublisherBase
    {
        private readonly FileTransportSettings _fileTransportSettings;
        private static readonly object _fileLock = new object();

        public FileAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _fileTransportSettings = (FileTransportSettings)alertTransportSettings;
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                WriteToFile(ownedEntry);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    WriteToFile(interceptedEntry);
                }
            }

            return Task.CompletedTask;
        }

        private void WriteToFile(KeyValuePair<string, HealthReportEntry> entry)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var filePath = GetCurrentFilePath();

                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var logLine = FormatLogLine(healthCheckData);

                lock (_fileLock)
                {
                    CheckAndRotateFile(filePath);
                    File.AppendAllText(filePath, logLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to write file alert");
            }
        }

        private string GetCurrentFilePath()
        {
            var basePath = _fileTransportSettings.FilePath;
            var rollingPolicy = _fileTransportSettings.RollingPolicy ?? "daily";

            if (rollingPolicy.Equals("daily", StringComparison.OrdinalIgnoreCase))
            {
                var ext = Path.GetExtension(basePath);
                var nameWithoutExt = Path.Combine(
                    Path.GetDirectoryName(basePath) ?? "",
                    Path.GetFileNameWithoutExtension(basePath));
                return $"{nameWithoutExt}-{DateTime.Now:yyyy-MM-dd}{ext}";
            }

            return basePath;
        }

        private void CheckAndRotateFile(string filePath)
        {
            if (_fileTransportSettings.MaxFileSizeBytes <= 0) return;
            if (!File.Exists(filePath)) return;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length >= _fileTransportSettings.MaxFileSizeBytes)
            {
                var rotatedPath = $"{filePath}.{DateTime.Now:yyyyMMddHHmmss}.bak";
                File.Move(filePath, rotatedPath);
            }
        }

        private static string FormatLogLine(HealthCheckData data)
        {
            var statusIcon = data.Status switch
            {
                Models.HealthStatus.Unhealthy => "FAIL",
                Models.HealthStatus.Degraded => "WARN",
                Models.HealthStatus.Healthy => "OK",
                _ => "UNKNOWN"
            };

            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{statusIcon}] " +
                   $"Name={data.Name} | ServiceType={data.ServiceType} | " +
                   $"Duration={data.Duration}ms | Machine={data.MachineName} | " +
                   $"Description={data.Description} | Error={data.CheckError}";
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<FileTransportValidationError>()
                .Requires(_fileTransportSettings.FilePath)
                .IsNotNullOrEmpty();
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
