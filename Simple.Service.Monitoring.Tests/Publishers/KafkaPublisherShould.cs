using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.KafkaPublisher;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class KafkaPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private KafkaTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new KafkaTransportSettings
            {
                Name = "KafkaPublisherTest",
                BootstrapServers = "localhost:9092",
                Topic = "health-check-alerts",
                ClientId = "monitoring-service"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("kafka_pub_test", "KafkaPublisherTest", AlertTransportMethod.KafkaTransport);
            var publisher = new KafkaAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_BootstrapServers_IsNull()
        {
            var invalidSettings = new KafkaTransportSettings { Name = "Invalid", BootstrapServers = null };
            var healthCheck = CreateHealthCheck("kafka_test", "Invalid", AlertTransportMethod.KafkaTransport);

            Assert.Throws<KafkaTransportValidationError>(() =>
            {
                var publisher = new KafkaAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_BootstrapServers_IsEmpty()
        {
            var invalidSettings = new KafkaTransportSettings { Name = "Invalid", BootstrapServers = "" };
            var healthCheck = CreateHealthCheck("kafka_test", "Invalid", AlertTransportMethod.KafkaTransport);

            Assert.Throws<KafkaTransportValidationError>(() =>
            {
                var publisher = new KafkaAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        [Category("Integration")]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("kafka_pub", "KafkaPublisherTest", AlertTransportMethod.KafkaTransport);
            var publisher = new KafkaAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("kafka_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        [Category("Integration")]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("kafka_fail", "KafkaPublisherTest", AlertTransportMethod.KafkaTransport);
            var publisher = new KafkaAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("kafka_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public void SetUp_Should_NotThrow_WithMultipleBrokers()
        {
            var multiBrokerSettings = new KafkaTransportSettings
            {
                Name = "KafkaMulti",
                BootstrapServers = "broker1:9092,broker2:9092,broker3:9092",
                Topic = "monitoring-results",
                ClientId = "multi-broker-client"
            };
            var healthCheck = CreateHealthCheck("kafka_multi", "KafkaMulti", AlertTransportMethod.KafkaTransport);
            var publisher = new KafkaAlertingPublisher(_healthChecksBuilder, healthCheck, multiBrokerSettings);

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
