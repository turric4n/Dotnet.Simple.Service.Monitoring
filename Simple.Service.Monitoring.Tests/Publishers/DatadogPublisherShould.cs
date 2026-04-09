using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Datadog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class DatadogPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private DatadogTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new DatadogTransportSettings
            {
                Name = "DatadogTest",
                ApiKey = "dd-api-key-12345",
                ApplicationKey = "dd-app-key-67890",
                Site = "datadoghq.com"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("dd_test", "DatadogTest", AlertTransportMethod.Datadog);
            var publisher = new DatadogAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_ApiKey_IsNull()
        {
            var invalidSettings = new DatadogTransportSettings { Name = "Invalid", ApiKey = null };
            var healthCheck = CreateHealthCheck("dd_test", "Invalid", AlertTransportMethod.Datadog);

            Assert.Throws<DatadogValidationError>(() =>
            {
                var publisher = new DatadogAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_ApiKey_IsEmpty()
        {
            var invalidSettings = new DatadogTransportSettings { Name = "Invalid", ApiKey = "" };
            var healthCheck = CreateHealthCheck("dd_test", "Invalid", AlertTransportMethod.Datadog);

            Assert.Throws<DatadogValidationError>(() =>
            {
                var publisher = new DatadogAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("dd_pub", "DatadogTest", AlertTransportMethod.Datadog);
            var publisher = new DatadogAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("dd_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("dd_fail", "DatadogTest", AlertTransportMethod.Datadog);
            var publisher = new DatadogAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("dd_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public async Task Should_HandleDegradedStatus()
        {
            var healthCheck = CreateHealthCheck("dd_degraded", "DatadogTest", AlertTransportMethod.Datadog);
            var publisher = new DatadogAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("dd_degraded", HealthStatus.Degraded, "Slow response");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
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
