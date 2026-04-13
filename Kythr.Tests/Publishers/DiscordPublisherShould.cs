using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Exceptions.AlertBehaviour;
using Kythr.Library.Monitoring.Implementations.Publishers.Discord;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Kythr.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    public class DiscordPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private DiscordTransportSettings _settings;

        [SetUp]
        public void Setup()
        {
            var mock = new Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;

            _settings = new DiscordTransportSettings
            {
                Name = "DiscordTest",
                WebhookUrl = "https://discord.com/api/webhooks/123456/abcdef",
                Username = "HealthBot",
                AvatarUrl = "https://example.com/avatar.png"
            };
        }

        [Test]
        public void SetUp_Should_NotThrow_WithValidConfiguration()
        {
            var healthCheck = CreateHealthCheck("discord_test", "DiscordTest", AlertTransportMethod.Discord);
            var publisher = new DiscordAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);

            Action act = () => publisher.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Validate_Should_Throw_When_WebhookUrl_IsNull()
        {
            var invalidSettings = new DiscordTransportSettings { Name = "Invalid", WebhookUrl = null };
            var healthCheck = CreateHealthCheck("discord_test", "Invalid", AlertTransportMethod.Discord);

            Assert.Throws<DiscordValidationError>(() =>
            {
                var publisher = new DiscordAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_Throw_When_WebhookUrl_IsEmpty()
        {
            var invalidSettings = new DiscordTransportSettings { Name = "Invalid", WebhookUrl = "" };
            var healthCheck = CreateHealthCheck("discord_test", "Invalid", AlertTransportMethod.Discord);

            Assert.Throws<DiscordValidationError>(() =>
            {
                var publisher = new DiscordAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidReport()
        {
            var healthCheck = CreateHealthCheck("discord_pub", "DiscordTest", AlertTransportMethod.Discord);
            var publisher = new DiscordAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("discord_pub", HealthStatus.Unhealthy, "Service down");

            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public async Task Should_IncrementFailCount_OnUnhealthyReport()
        {
            var healthCheck = CreateHealthCheck("discord_fail", "DiscordTest", AlertTransportMethod.Discord);
            var publisher = new DiscordAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("discord_fail", HealthStatus.Unhealthy, "Down");

            await publisher.PublishAsync(report, CancellationToken.None);
            await Task.Delay(100);

            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public async Task Should_HandleDegradedStatus()
        {
            var healthCheck = CreateHealthCheck("discord_degraded", "DiscordTest", AlertTransportMethod.Discord);
            var publisher = new DiscordAlertingPublisher(_healthChecksBuilder, healthCheck, _settings);
            var report = CreateHealthReport("discord_degraded", HealthStatus.Degraded, "Slow");

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
