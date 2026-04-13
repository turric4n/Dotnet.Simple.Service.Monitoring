using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using Kythr.Library.Monitoring.Implementations.Publishers.PagerDuty;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Kythr.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class PagerDutyPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private PagerDutyTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new PagerDutyTransportSettings
            {
                Name = "PagerDutyTest",
                RoutingKey = "e93facc04764012d7bfb002500d5d1a6",
                Severity = "error"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("pd_test", "PagerDutyTest", AlertTransportMethod.PagerDuty);
            var publisher = new PagerDutyAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_RoutingKey_IsNull()
        {
            var invalidSettings = new PagerDutyTransportSettings { Name = "Invalid", RoutingKey = null };
            var healthCheck = CreateHealthCheck("pd_test", "Invalid", AlertTransportMethod.PagerDuty);

            Assert.Throws<PagerDutyValidationError>(() =>
            {
                var publisher = new PagerDutyAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_RoutingKey_IsEmpty()
        {
            var invalidSettings = new PagerDutyTransportSettings { Name = "Invalid", RoutingKey = "" };
            var healthCheck = CreateHealthCheck("pd_test", "Invalid", AlertTransportMethod.PagerDuty);

            Assert.Throws<PagerDutyValidationError>(() =>
            {
                var publisher = new PagerDutyAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("pd_pub", "PagerDutyTest", AlertTransportMethod.PagerDuty);
            var publisher = new PagerDutyAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("pd_pub", HealthStatus.Unhealthy, "Service down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("pd_fail", "PagerDutyTest", AlertTransportMethod.PagerDuty);
            var publisher = new PagerDutyAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("pd_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public async Task Should_ResetFailCount_OnRecovery()
        {
            var healthCheck = CreateHealthCheck("pd_recover", "PagerDutyTest", AlertTransportMethod.PagerDuty);
            healthCheck.AlertBehaviour[0].AlertOnServiceRecovered = true;

            var publisher = new PagerDutyAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            var unhealthy = CreateHealthReport("pd_recover", HealthStatus.Unhealthy, "Down");
            await publisher.PublishAsync(unhealthy, CancellationToken.None);
            await Task.Delay(100);

            var healthy = CreateHealthReport("pd_recover", HealthStatus.Healthy, "Recovered");
            await publisher.PublishAsync(healthy, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(0);
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
