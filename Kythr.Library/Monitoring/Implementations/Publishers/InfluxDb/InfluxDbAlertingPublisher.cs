using CuttingEdge.Conditions;
using InfluxDB.Collector;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Abstractions;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kythr.Library.Monitoring.Implementations.Publishers.InfluxDB
{
    public class InfluxDbAlertingPublisher : PublisherBase
    {
        private readonly InfluxDbTransportSettings _influxDBTransportSettings;
        private static readonly HttpClient _httpClient = new HttpClient();
        private bool _databaseEnsured = false;

        public InfluxDbAlertingPublisher(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings) : 
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _influxDBTransportSettings = (InfluxDbTransportSettings)alertTransportSettings;
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                if (_influxDBTransportSettings.AutoCreateDatabase && !_databaseEnsured)
                {
                    await EnsureDatabaseExistsAsync(cancellationToken);
                    _databaseEnsured = true;
                }

                var ownedEntry = this.GetOwnedEntry(report);

                var interceptedEntries = this.GetInterceptedEntries(report);

                var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

                if (ownedAlerting)
                {
                    SendEntryToInflux(ownedEntry);
                }

                foreach (var entry in interceptedEntries)
                {
                    if (this.IsOkToAlert(entry, false))
                    {
                        SendEntryToInflux(entry);
                    }
                }

            }, cancellationToken);

            return Task.CompletedTask;
        }

        private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var host = _influxDBTransportSettings.Host.TrimEnd('/');

                if (_influxDBTransportSettings.Version == InfluxDbVersion.V2)
                {
                    await EnsureBucketExistsV2Async(host, cancellationToken);
                }
                else
                {
                    await EnsureDatabaseExistsV1Async(host, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to auto-create InfluxDB database");
            }
        }

        private async Task EnsureDatabaseExistsV1Async(string host, CancellationToken cancellationToken)
        {
            var retentionClause = !string.IsNullOrEmpty(_influxDBTransportSettings.RetentionDuration)
                ? $" WITH DURATION {_influxDBTransportSettings.RetentionDuration}"
                : "";

            var retentionPolicyClause = !string.IsNullOrEmpty(_influxDBTransportSettings.RetentionPolicy)
                ? $" NAME \"{_influxDBTransportSettings.RetentionPolicy}\""
                : "";

            var query = Uri.EscapeDataString(
                $"CREATE DATABASE \"{_influxDBTransportSettings.Database}\"{retentionClause}{retentionPolicyClause}");

            var url = $"{host}/query?q={query}";
            var response = await _httpClient.PostAsync(url, null, cancellationToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task EnsureBucketExistsV2Async(string host, CancellationToken cancellationToken)
        {
            var retentionSeconds = ParseRetentionDuration(_influxDBTransportSettings.RetentionDuration);
            var payload = new
            {
                name = _influxDBTransportSettings.Database,
                orgID = _influxDBTransportSettings.Organization,
                retentionRules = new[]
                {
                    new { type = "expire", everySeconds = retentionSeconds }
                }
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{host}/api/v2/buckets");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            if (!string.IsNullOrEmpty(_influxDBTransportSettings.Token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Token", _influxDBTransportSettings.Token);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            // 422 means bucket already exists, which is fine
            if (!response.IsSuccessStatusCode && (int)response.StatusCode != 422)
            {
                response.EnsureSuccessStatusCode();
            }
        }

        private static int ParseRetentionDuration(string duration)
        {
            if (string.IsNullOrEmpty(duration)) return 0; // 0 = infinite retention

            // Parse simple duration formats like "7d", "30d", "1h", "24h"
            if (duration.EndsWith("d") && int.TryParse(duration.TrimEnd('d'), out var days))
                return days * 86400;
            if (duration.EndsWith("h") && int.TryParse(duration.TrimEnd('h'), out var hours))
                return hours * 3600;
            if (duration.EndsWith("m") && int.TryParse(duration.TrimEnd('m'), out var minutes))
                return minutes * 60;
            if (duration.EndsWith("s") && int.TryParse(duration.TrimEnd('s'), out var secs))
                return secs;

            return 0;
        }

        private void SendEntryToInflux(KeyValuePair<string, HealthReportEntry> healthReportEntry)
        {
            using var collector = new CollectorConfiguration()
                .Tag.With("name", _healthCheck.Name)
                .Batch.AtInterval(TimeSpan.FromSeconds(2))
                .WriteTo.InfluxDB(_influxDBTransportSettings.Host, _influxDBTransportSettings.Database)
                .CreateCollector();


            var tags = new Dictionary<string, string>()
            {
                { "endpoint", _healthCheck.EndpointOrHost ?? _healthCheck.ConnectionString }
            };

            var fields = new Dictionary<string, object>()
            {
                { "status", (int)healthReportEntry.Value.Status },
                { "error", healthReportEntry.Value.Exception },
                { "responsetime", healthReportEntry.Value.Duration.Milliseconds },
                { "description", healthReportEntry.Value.Description },
            };

            foreach (var valueTag in healthReportEntry.Value.Data)
            {
                fields.Add(valueTag.Key, valueTag.Value);
            }

            collector.Write("health_check", fields, tags);
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<InfluxDbValidationError>()
                .Requires(_influxDBTransportSettings.Host)
                .IsNotNullOrEmpty();

            Condition.WithExceptionOnFailure<InfluxDbValidationError>()
                .Requires(_influxDBTransportSettings.Database)
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
