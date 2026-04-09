using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Teams;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class TeamsPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private TeamsTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new TeamsTransportSettings
            {
                Name = "TeamsTest",
                WebhookUrl = "https://outlook.office.com/webhook/test-guid"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("teams_test", "TeamsTest", AlertTransportMethod.Teams);
            var publisher = new TeamsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_WebhookUrl_IsNull()
        {
            var invalidSettings = new TeamsTransportSettings { Name = "InvalidTeams", WebhookUrl = null };
            var healthCheck = CreateHealthCheck("teams_test", "InvalidTeams", AlertTransportMethod.Teams);

            Assert.Throws<TeamsValidationError>(() =>
            {
                var publisher = new TeamsAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_WebhookUrl_IsEmpty()
        {
            var invalidSettings = new TeamsTransportSettings { Name = "InvalidTeams", WebhookUrl = "" };
            var healthCheck = CreateHealthCheck("teams_test", "InvalidTeams", AlertTransportMethod.Teams);

            Assert.Throws<TeamsValidationError>(() =>
            {
                var publisher = new TeamsAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("teams_publish", "TeamsTest", AlertTransportMethod.Teams);
            var publisher = new TeamsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("teams_publish", HealthStatus.Unhealthy, "Service down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("teams_fail", "TeamsTest", AlertTransportMethod.Teams);
            var publisher = new TeamsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("teams_fail", HealthStatus.Unhealthy, "Service down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public async Task Should_ResetFailCount_OnRecovery()
        {
            var healthCheck = CreateHealthCheck("teams_recover", "TeamsTest", AlertTransportMethod.Teams);
            healthCheck.AlertBehaviour[0].AlertOnServiceRecovered = true;

            var publisher = new TeamsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            var unhealthy = CreateHealthReport("teams_recover", HealthStatus.Unhealthy, "Down");
            await publisher.PublishAsync(unhealthy, CancellationToken.None);
            await Task.Delay(100);
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);

            var healthy = CreateHealthReport("teams_recover", HealthStatus.Healthy, "Recovered");
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
