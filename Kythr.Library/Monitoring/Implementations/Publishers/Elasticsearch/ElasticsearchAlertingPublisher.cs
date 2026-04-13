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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;

namespace Kythr.Library.Monitoring.Implementations.Publishers.Elasticsearch
{
    public class ElasticsearchAlertingPublisher : PublisherBase, IDisposable
    {
        private readonly ElasticsearchTransportSettings _elasticsearchTransportSettings;
        private ElasticsearchClient _client;
        private bool _disposed = false;

        public ElasticsearchAlertingPublisher(IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings) :
            base(healthChecksBuilder, healthCheck, alertTransportSettings)
        {
            _elasticsearchTransportSettings = (ElasticsearchTransportSettings)alertTransportSettings;
        }

        private ElasticsearchClient GetClient()
        {
            if (_client != null) return _client;

            var nodes = _elasticsearchTransportSettings.Nodes
                .Select(n => new Uri(n))
                .ToArray();

            var settings = new ElasticsearchClientSettings(new StaticNodePool(nodes));

            if (!string.IsNullOrEmpty(_elasticsearchTransportSettings.Username) &&
                !string.IsNullOrEmpty(_elasticsearchTransportSettings.Password))
            {
                settings.Authentication(new BasicAuthentication(
                    _elasticsearchTransportSettings.Username,
                    _elasticsearchTransportSettings.Password));
            }

            _client = new ElasticsearchClient(settings);
            return _client;
        }

        public override async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var ownedEntry = this.GetOwnedEntry(report);
            var interceptedEntries = this.GetInterceptedEntries(report);
            var ownedAlerting = this.IsOkToAlert(ownedEntry, false);

            if (ownedAlerting)
            {
                await IndexHealthCheckAsync(ownedEntry, cancellationToken);
                AlertObservers(ownedEntry);
            }

            foreach (var interceptedEntry in interceptedEntries)
            {
                if (this.IsOkToAlert(interceptedEntry, true))
                {
                    await IndexHealthCheckAsync(interceptedEntry, cancellationToken);
                }
            }
        }

        private async Task IndexHealthCheckAsync(KeyValuePair<string, HealthReportEntry> entry, CancellationToken cancellationToken)
        {
            try
            {
                var healthCheckData = new HealthCheckData(entry.Value, entry.Key);
                var client = GetClient();
                var indexPrefix = _elasticsearchTransportSettings.IndexPrefix ?? "health-checks";
                var indexName = $"{indexPrefix}-{DateTime.UtcNow:yyyy.MM.dd}";

                var document = new
                {
                    timestamp = DateTime.UtcNow,
                    name = healthCheckData.Name,
                    status = healthCheckData.Status.ToString(),
                    statusCode = (int)healthCheckData.Status,
                    serviceType = healthCheckData.ServiceType.ToString(),
                    durationMs = healthCheckData.Duration,
                    machineName = healthCheckData.MachineName,
                    description = healthCheckData.Description,
                    error = healthCheckData.CheckError,
                    tags = healthCheckData.Tags
                };

                await client.IndexAsync(document, (Elastic.Clients.Elasticsearch.IndexName)indexName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to index health check to Elasticsearch");
            }
        }

        protected internal override void Validate()
        {
            Condition.WithExceptionOnFailure<ElasticsearchValidationError>()
                .Requires(_elasticsearchTransportSettings.Nodes)
                .IsNotNull();

            Condition.WithExceptionOnFailure<ElasticsearchValidationError>()
                .Requires(_elasticsearchTransportSettings.Nodes.Length)
                .IsGreaterThan(0);
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // ElasticsearchClient uses an internal transport that should be released
                _client = null;
            }
            _disposed = true;
        }

        ~ElasticsearchAlertingPublisher()
        {
            Dispose(false);
        }
    }
}
