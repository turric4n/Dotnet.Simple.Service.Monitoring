using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using Kythr.Library.Monitoring.Implementations.Publishers.Elasticsearch;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Kythr.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class ElasticsearchPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private ElasticsearchTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new ElasticsearchTransportSettings
            {
                Name = "ElasticsearchTest",
                Nodes = new[] { "http://localhost:9200" },
                IndexPrefix = "health-checks",
                Username = "elastic",
                Password = "changeme"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("es_test", "ElasticsearchTest", AlertTransportMethod.Elasticsearch);
            var publisher = new ElasticsearchAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_Nodes_IsNull()
        {
            var invalidSettings = new ElasticsearchTransportSettings { Name = "Invalid", Nodes = null };
            var healthCheck = CreateHealthCheck("es_test", "Invalid", AlertTransportMethod.Elasticsearch);

            Assert.Throws<ElasticsearchValidationError>(() =>
            {
                var publisher = new ElasticsearchAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_Nodes_IsEmpty()
        {
            var invalidSettings = new ElasticsearchTransportSettings { Name = "Invalid", Nodes = new string[0] };
            var healthCheck = CreateHealthCheck("es_test", "Invalid", AlertTransportMethod.Elasticsearch);

            Assert.Throws<ElasticsearchValidationError>(() =>
            {
                var publisher = new ElasticsearchAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        [Category("Integration")]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("es_pub", "ElasticsearchTest", AlertTransportMethod.Elasticsearch);
            var publisher = new ElasticsearchAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("es_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        [Category("Integration")]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("es_fail", "ElasticsearchTest", AlertTransportMethod.Elasticsearch);
            var publisher = new ElasticsearchAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("es_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public void SetUp_Should_NotThrow_WithMultipleNodes()
        {
            var multiNodeSettings = new ElasticsearchTransportSettings
            {
                Name = "ESMulti",
                Nodes = new[] { "http://node1:9200", "http://node2:9200", "http://node3:9200" },
                IndexPrefix = "monitoring"
            };
            var healthCheck = CreateHealthCheck("es_multi", "ESMulti", AlertTransportMethod.Elasticsearch);
            var publisher = new ElasticsearchAlertingPublisher(_healthChecksBuilder, healthCheck, multiNodeSettings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        #region Helpers

        private ServiceHealthCheck CreateHealthCheck(string name, string transportName, AlertTransportMethod method)
        {
            return new ServiceHealthCheck
            {
                Name = name,
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                Alert = true,
                AlertBehaviour = new List<AlertBehaviour>
                {
                    new AlertBehaviour
                    {
                        TransportName = transportName,
                        TransportMethod = method,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromMinutes(5)
                    }
                }
            };
        }

        private HealthReport CreateHealthReport(string name, HealthStatus status, string description)
        {
            var entry = new HealthReportEntry(status, description, TimeSpan.FromMilliseconds(100), null, null);
            return new HealthReport(new Dictionary<string, HealthReportEntry> { { name, entry } }, TimeSpan.FromMilliseconds(100));
        }

        #endregion
    }
}
