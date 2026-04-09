using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class RmqPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private RmqTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new RmqTransportSettings
            {
                Name = "RmqPublisherTest",
                ConnectionString = "amqp://guest:guest@localhost:5672/",
                Exchange = "health_checks",
                RoutingKey = "health.check.result",
                QueueName = "health-check-alerts"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("rmq_pub_test", "RmqPublisherTest", AlertTransportMethod.RabbitMq);
            var publisher = new RmqAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_ConnectionString_IsNull()
        {
            var invalidSettings = new RmqTransportSettings { Name = "Invalid", ConnectionString = null };
            var healthCheck = CreateHealthCheck("rmq_test", "Invalid", AlertTransportMethod.RabbitMq);

            Assert.Throws<RmqTransportValidationError>(() =>
            {
                var publisher = new RmqAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_ConnectionString_IsEmpty()
        {
            var invalidSettings = new RmqTransportSettings { Name = "Invalid", ConnectionString = "" };
            var healthCheck = CreateHealthCheck("rmq_test", "Invalid", AlertTransportMethod.RabbitMq);

            Assert.Throws<RmqTransportValidationError>(() =>
            {
                var publisher = new RmqAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("rmq_pub", "RmqPublisherTest", AlertTransportMethod.RabbitMq);
            var publisher = new RmqAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("rmq_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("rmq_fail", "RmqPublisherTest", AlertTransportMethod.RabbitMq);
            var publisher = new RmqAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("rmq_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public void SetUp_Should_NotThrow_WithCustomExchange()
        {
            var customSettings = new RmqTransportSettings
            {
                Name = "RmqCustom",
                ConnectionString = "amqp://user:pass@rabbitmq.host:5672/vhost",
                Exchange = "custom_exchange",
                RoutingKey = "monitoring.alerts",
                QueueName = "alert-queue"
            };
            var healthCheck = CreateHealthCheck("rmq_custom", "RmqCustom", AlertTransportMethod.RabbitMq);
            var publisher = new RmqAlertingPublisher(_healthChecksBuilder, healthCheck, customSettings);

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
