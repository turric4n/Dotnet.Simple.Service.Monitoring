using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Opsgenie;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class OpsgeniePublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private OpsgenieTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new OpsgenieTransportSettings
            {
                Name = "OpsgenieTest",
                ApiKey = "test-api-key-12345",
                Priority = "P2"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("ops_test", "OpsgenieTest", AlertTransportMethod.Opsgenie);
            var publisher = new OpsgenieAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_ApiKey_IsNull()
        {
            var invalidSettings = new OpsgenieTransportSettings { Name = "Invalid", ApiKey = null };
            var healthCheck = CreateHealthCheck("ops_test", "Invalid", AlertTransportMethod.Opsgenie);

            Assert.Throws<OpsgenieValidationError>(() =>
            {
                var publisher = new OpsgenieAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_ApiKey_IsEmpty()
        {
            var invalidSettings = new OpsgenieTransportSettings { Name = "Invalid", ApiKey = "" };
            var healthCheck = CreateHealthCheck("ops_test", "Invalid", AlertTransportMethod.Opsgenie);

            Assert.Throws<OpsgenieValidationError>(() =>
            {
                var publisher = new OpsgenieAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("ops_pub", "OpsgenieTest", AlertTransportMethod.Opsgenie);
            var publisher = new OpsgenieAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("ops_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("ops_fail", "OpsgenieTest", AlertTransportMethod.Opsgenie);
            var publisher = new OpsgenieAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("ops_fail", HealthStatus.Unhealthy, "Down");

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
