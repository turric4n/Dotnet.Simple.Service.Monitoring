using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using Kythr.Library.Monitoring.Implementations.Publishers.AppInsights;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Kythr.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class AppInsightsPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private AppInsightsTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new AppInsightsTransportSettings
            {
                Name = "AppInsightsTest",
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000;IngestionEndpoint=https://dc.applicationinsights.azure.com/"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithConnectionString()
        {
            var healthCheck = CreateHealthCheck("ai_test", "AppInsightsTest", AlertTransportMethod.AppInsights);
            var publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void SetUp_Should_NotThrow_WithInstrumentationKey()
        {
            var ikSettings = new AppInsightsTransportSettings
            {
                Name = "AppInsightsIK",
                InstrumentationKey = "00000000-0000-0000-0000-000000000000"
            };
            var healthCheck = CreateHealthCheck("ai_ik", "AppInsightsIK", AlertTransportMethod.AppInsights);
            var publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, healthCheck, ikSettings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_Both_ConnectionString_And_Key_AreNull()
        {
            var invalidSettings = new AppInsightsTransportSettings
            {
                Name = "Invalid",
                ConnectionString = null,
                InstrumentationKey = null
            };
            var healthCheck = CreateHealthCheck("ai_test", "Invalid", AlertTransportMethod.AppInsights);

            Assert.Throws<AppInsightsValidationError>(() =>
            {
                var publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_Both_ConnectionString_And_Key_AreEmpty()
        {
            var invalidSettings = new AppInsightsTransportSettings
            {
                Name = "Invalid",
                ConnectionString = "",
                InstrumentationKey = ""
            };
            var healthCheck = CreateHealthCheck("ai_test", "Invalid", AlertTransportMethod.AppInsights);

            Assert.Throws<AppInsightsValidationError>(() =>
            {
                var publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("ai_pub", "AppInsightsTest", AlertTransportMethod.AppInsights);
            var publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("ai_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("ai_fail", "AppInsightsTest", AlertTransportMethod.AppInsights);
            var publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("ai_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public async Task Should_ResetFailCount_OnRecovery()
        {
            var healthCheck = CreateHealthCheck("ai_recover", "AppInsightsTest", AlertTransportMethod.AppInsights);
            healthCheck.AlertBehaviour[0].AlertOnServiceRecovered = true;

            var publisher = new AppInsightsAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            var unhealthy = CreateHealthReport("ai_recover", HealthStatus.Unhealthy, "Down");
            await publisher.PublishAsync(unhealthy, CancellationToken.None);
            await Task.Delay(100);

            var healthy = CreateHealthReport("ai_recover", HealthStatus.Healthy, "Up");
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
