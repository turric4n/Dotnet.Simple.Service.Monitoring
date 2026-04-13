using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using Kythr.Library.Monitoring.Implementations.Publishers.FileLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Kythr.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class FilePublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private FileTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new FileTransportSettings
            {
                Name = "FileTest",
                FilePath = Path.Combine(Path.GetTempPath(), "health-checks.log"),
                MaxFileSizeBytes = 10485760,
                RollingPolicy = "daily"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("file_test", "FileTest", AlertTransportMethod.FileLog);
            var publisher = new FileAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_FilePath_IsNull()
        {
            var invalidSettings = new FileTransportSettings { Name = "Invalid", FilePath = null };
            var healthCheck = CreateHealthCheck("file_test", "Invalid", AlertTransportMethod.FileLog);

            Assert.Throws<FileTransportValidationError>(() =>
            {
                var publisher = new FileAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_FilePath_IsEmpty()
        {
            var invalidSettings = new FileTransportSettings { Name = "Invalid", FilePath = "" };
            var healthCheck = CreateHealthCheck("file_test", "Invalid", AlertTransportMethod.FileLog);

            Assert.Throws<FileTransportValidationError>(() =>
            {
                var publisher = new FileAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("file_pub", "FileTest", AlertTransportMethod.FileLog);
            var publisher = new FileAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("file_pub", HealthStatus.Unhealthy, "Down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("file_fail", "FileTest", AlertTransportMethod.FileLog);
            var publisher = new FileAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("file_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public void SetUp_Should_NotThrow_WithCustomRollingPolicy()
        {
            var customSettings = new FileTransportSettings
            {
                Name = "FileCustom",
                FilePath = Path.Combine(Path.GetTempPath(), "monitoring.log"),
                MaxFileSizeBytes = 5242880,
                RollingPolicy = "hourly"
            };
            var healthCheck = CreateHealthCheck("file_custom", "FileCustom", AlertTransportMethod.FileLog);
            var publisher = new FileAlertingPublisher(_healthChecksBuilder, healthCheck, customSettings);

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
