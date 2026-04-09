using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Console;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class ConsolePublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private ConsoleTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new ConsoleTransportSettings
            {
                Name = "ConsoleTest",
                UseColors = true,
                OutputFormat = "text"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithDefaultSettings()
        {
            var healthCheck = CreateHealthCheck("console_test", "ConsoleTest", AlertTransportMethod.Console);
            var publisher = new ConsoleAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void SetUp_Should_NotThrow_WithJsonFormat()
        {
            var jsonSettings = new ConsoleTransportSettings
            {
                Name = "ConsoleJson",
                UseColors = false,
                OutputFormat = "json"
            };
            var healthCheck = CreateHealthCheck("console_json", "ConsoleJson", AlertTransportMethod.Console);
            var publisher = new ConsoleAlertingPublisher(_healthChecksBuilder, healthCheck, jsonSettings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("console_pub", "ConsoleTest", AlertTransportMethod.Console);
            var publisher = new ConsoleAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("console_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("console_fail", "ConsoleTest", AlertTransportMethod.Console);
            var publisher = new ConsoleAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("console_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public async Task Should_HandleHealthyReport()
        {
            var healthCheck = CreateHealthCheck("console_healthy", "ConsoleTest", AlertTransportMethod.Console);
            var publisher = new ConsoleAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("console_healthy", HealthStatus.Healthy, "OK");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(0);
        }

        [Test]
        public async Task Should_HandleDegradedStatus()
        {
            var healthCheck = CreateHealthCheck("console_degraded", "ConsoleTest", AlertTransportMethod.Console);
            var publisher = new ConsoleAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("console_degraded", HealthStatus.Degraded, "Slow");

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
