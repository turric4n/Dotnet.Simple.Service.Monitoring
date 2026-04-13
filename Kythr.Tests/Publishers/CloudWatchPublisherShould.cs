using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using Kythr.Library.Monitoring.Implementations.Publishers.CloudWatch;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Kythr.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class CloudWatchPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private CloudWatchTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new CloudWatchTransportSettings
            {
                Name = "CloudWatchTest",
                Region = "us-east-1",
                AccessKey = "AKIAIOSFODNN7EXAMPLE",
                SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                Namespace = "HealthChecks"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("cw_test", "CloudWatchTest", AlertTransportMethod.CloudWatch);
            var publisher = new CloudWatchAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_Region_IsNull()
        {
            var invalidSettings = new CloudWatchTransportSettings { Name = "Invalid", Region = null };
            var healthCheck = CreateHealthCheck("cw_test", "Invalid", AlertTransportMethod.CloudWatch);

            Assert.Throws<CloudWatchValidationError>(() =>
            {
                var publisher = new CloudWatchAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_Region_IsEmpty()
        {
            var invalidSettings = new CloudWatchTransportSettings { Name = "Invalid", Region = "" };
            var healthCheck = CreateHealthCheck("cw_test", "Invalid", AlertTransportMethod.CloudWatch);

            Assert.Throws<CloudWatchValidationError>(() =>
            {
                var publisher = new CloudWatchAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("cw_pub", "CloudWatchTest", AlertTransportMethod.CloudWatch);
            var publisher = new CloudWatchAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("cw_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("cw_fail", "CloudWatchTest", AlertTransportMethod.CloudWatch);
            var publisher = new CloudWatchAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("cw_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public void SetUp_Should_NotThrow_WithIAMAuth()
        {
            var iamSettings = new CloudWatchTransportSettings
            {
                Name = "CloudWatchIAM",
                Region = "eu-west-1",
                Namespace = "ProductionChecks"
            };
            var healthCheck = CreateHealthCheck("cw_iam", "CloudWatchIAM", AlertTransportMethod.CloudWatch);
            var publisher = new CloudWatchAlertingPublisher(_healthChecksBuilder, healthCheck, iamSettings);

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
