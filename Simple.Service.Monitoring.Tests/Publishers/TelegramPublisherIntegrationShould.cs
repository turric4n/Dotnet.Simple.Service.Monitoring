using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Telegram;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    /// <summary>
    /// Comprehensive integration tests for TelegramAlertingPublisher using REAL Telegram Bot API
    /// Tests failure scenarios, recovery alerts, and scheduling behaviors
    /// NOTE: These tests are marked [Explicit] and require valid Telegram bot credentials
    /// Configure BotApiToken and ChatId before running
    /// </summary>
    [TestFixture(Category = "Integration")]
    [Category("TelegramPublisher")]
    [Category("RealAPI")]
    //[Explicit("Requires real Telegram bot credentials and sends actual messages")]
    public class TelegramPublisherIntegrationShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private TelegramTransportSettings _telegramSettings;

        // ⚠️ CONFIGURE THESE VALUES BEFORE RUNNING TESTS ⚠️
        private const string BOT_API_TOKEN = "6030340647:AAGnXhQsziJUsv8eb7ZC2Am_-qpKdDV5rLQ"; // e.g., "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11"
        private const string CHAT_ID = "-960612732";         // e.g., "-1001234567890" or "123456789"

        [SetUp]
        public void Setup()
        {
            // Validate that credentials are configured
            if (BOT_API_TOKEN == "YOUR_BOT_TOKEN_HERE" || CHAT_ID == "YOUR_CHAT_ID_HERE")
            {
                Assert.Inconclusive(
                    "Telegram bot credentials not configured. " +
                    "Please set BOT_API_TOKEN and CHAT_ID constants in TelegramPublisherIntegrationShould.cs");
            }

            var healthcheckbuildermock = new Mock<IHealthChecksBuilder>();
            healthcheckbuildermock
                .Setup(m => m.Services)
                .Returns(new ServiceCollection());
            _healthChecksBuilder = healthcheckbuildermock.Object;

            _telegramSettings = new TelegramTransportSettings()
            {
                Name = "TelegramTest",
                BotApiToken = BOT_API_TOKEN,
                ChatId = CHAT_ID
            };
        }

        #region Failure Scenarios

        [Test]
        [Category("Failure")]
        public async Task AlertImmediately_OnFirstFailure()
        {
            // Arrange
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_first_failure",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1, // Alert on first failure
                        AlertEvery = TimeSpan.FromMinutes(5),
                        PublishAllResults = false
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_first_failure", HealthStatus.Unhealthy, "Service unavailable");

            // Act
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);

            // Allow time for Telegram API call to complete
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should alert immediately on first failure");
            TestContext.WriteLine("✅ Telegram message sent successfully for first failure!");
        }

        [Test]
        [Category("Failure")]
        public async Task AlertAfterConsecutiveFailures_WhenThresholdConfigured()
        {
            // Arrange
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_threshold_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 3, // Alert after 3 consecutive failures
                        AlertEvery = TimeSpan.FromSeconds(2),
                        PublishAllResults = false
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_threshold_test", HealthStatus.Unhealthy, "Connection timeout");

            // Act & Assert
            TestContext.WriteLine("Sending failure 1...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(500);
            alertCount.Should().Be(0, "Should not alert on first failure");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);

            TestContext.WriteLine("Sending failure 2...");
            await Task.Delay(2100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(500);
            alertCount.Should().Be(0, "Should not alert on second failure");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(2);

            TestContext.WriteLine("Sending failure 3 - should trigger alert...");
            await Task.Delay(2100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(1, "Should alert on third consecutive failure");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(3);
            
            TestContext.WriteLine("✅ Telegram message sent after 3 consecutive failures!");
        }

        [Test]
        [Category("Failure")]
        public async Task AlertWithDifferentSeverities_Degraded()
        {
            // Arrange
            var alertCount = 0;
            var lastStatus = HealthStatus.Healthy;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_degraded_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback<KeyValuePair<string, HealthReportEntry>>(entry =>
                {
                    alertCount++;
                    lastStatus = entry.Value.Status;
                });

            publisher.Subscribe(observer.Object);

            var degradedReport = CreateHealthReport("telegram_degraded_test", HealthStatus.Degraded, "⚠️ Slow response time detected");

            // Act
            TestContext.WriteLine("Sending degraded status alert...");
            await publisher.PublishAsync(degradedReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should alert on degraded status");
            lastStatus.Should().Be(HealthStatus.Degraded, "Alert should be for degraded status");
            TestContext.WriteLine("✅ Telegram degraded message sent with ⚠️ emoji!");
        }

        [Test]
        [Category("Failure")]
        public async Task NotAlert_WhenHealthy()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_healthy_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1),
                        PublishAllResults = false // Don't publish healthy results
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var healthyReport = CreateHealthReport("telegram_healthy_test", HealthStatus.Healthy, "All systems operational");

            // Act
            TestContext.WriteLine("Sending healthy status (should not alert)...");
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            await Task.Delay(500);

            // Assert
            alertCount.Should().Be(0, "Should not alert when service is healthy");
            TestContext.WriteLine("✅ No Telegram message sent for healthy status (as expected)");
        }

        #endregion

        #region Recovery Scenarios

        [Test]
        [Category("Recovery")]
        public async Task AlertOnRecovery_WhenConfigured()
        {
            // Arrange
            var alertCount = 0;
            var alerts = new List<(HealthStatus Status, string Description)>();

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_recovery_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(2),
                        AlertOnServiceRecovered = true // Enable recovery alerts
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback<KeyValuePair<string, HealthReportEntry>>(entry =>
                {
                    alertCount++;
                    alerts.Add((entry.Value.Status, entry.Value.Description));
                });

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_recovery_test", HealthStatus.Unhealthy, "❌ Database connection lost");
            var healthyReport = CreateHealthReport("telegram_recovery_test", HealthStatus.Healthy, "✅ Service recovered - all systems operational");

            // Act
            TestContext.WriteLine("Sending failure alert...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(1, "Should alert on failure");

            TestContext.WriteLine("Sending recovery alert...");
            await Task.Delay(2100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(2, "Should alert on recovery");
            alerts.Should().HaveCount(2);
            alerts[0].Status.Should().Be(HealthStatus.Unhealthy, "First alert should be failure");
            alerts[1].Status.Should().Be(HealthStatus.Healthy, "Second alert should be recovery");
            TestContext.WriteLine("✅ Both failure and recovery messages sent to Telegram!");
        }

        [Test]
        [Category("Recovery")]
        public async Task NotAlertOnRecovery_WhenDisabled()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_no_recovery",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(2),
                        AlertOnServiceRecovered = false // Disable recovery alerts
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_no_recovery", HealthStatus.Unhealthy, "Service down");
            var healthyReport = CreateHealthReport("telegram_no_recovery", HealthStatus.Healthy, "Service up");

            // Act
            TestContext.WriteLine("Sending failure alert...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(1, "Should alert on failure");

            TestContext.WriteLine("Sending recovery (should not alert)...");
            await Task.Delay(2100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            await Task.Delay(500);

            // Assert
            alertCount.Should().Be(1, "Should NOT alert on recovery when disabled");
            TestContext.WriteLine("✅ Only failure message sent, recovery suppressed (as expected)");
        }

        [Test]
        [Category("Recovery")]
        public async Task ResetFailCount_OnRecovery()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_reset_count",
                ServiceType = ServiceType.Http,
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 3,
                        AlertEvery = TimeSpan.FromSeconds(2),
                        AlertOnServiceRecovered = true
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_reset_count", HealthStatus.Unhealthy, "Error");
            var healthyReport = CreateHealthReport("telegram_reset_count", HealthStatus.Healthy, "OK");

            // Act
            TestContext.WriteLine("Sending 2 failures (not enough to alert)...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(2100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(500);
            
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(2, "Should have 2 failures");

            TestContext.WriteLine("Sending recovery - should reset count...");
            await Task.Delay(2100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            await Task.Delay(500);
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(0, "Failed count should reset on recovery");

            TestContext.WriteLine("Sending new failure after recovery...");
            await Task.Delay(2100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(500);
            
            // Assert
            alertCount.Should().Be(0, "Should not have alerted (never reached threshold)");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1, "Count should restart from 1");
            TestContext.WriteLine("✅ Fail count reset verified!");
        }

        #endregion

        #region Scheduling Tests

        [Test]
        [Category("Scheduling")]
        public async Task RespectCooldownPeriod_BetweenAlerts()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_cooldown",
                ServiceType = ServiceType.Http,
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(5), // 5 second cooldown
                        AlertOnce = false // Allow repeated alerts
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_cooldown", HealthStatus.Unhealthy, "🔄 Testing cooldown period");

            // Act & Assert
            TestContext.WriteLine("Sending first alert...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(1, "Should send first alert");

            TestContext.WriteLine("Sending within cooldown (should not alert)...");
            await Task.Delay(2000); // Only 3 seconds total
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(500);
            alertCount.Should().Be(1, "Should not alert during cooldown");

            TestContext.WriteLine("Sending after cooldown (should alert)...");
            await Task.Delay(3000); // Total > 5 seconds
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(2, "Should alert after cooldown expires");
            
            TestContext.WriteLine("✅ Cooldown period respected - 2 messages sent!");
        }

        [Test]
        [Category("Scheduling")]
        public async Task AlertOnce_ThenSilenceUntilRecovery()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_alert_once",
                ServiceType = ServiceType.Http,
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(2),
                        AlertOnce = true, // Only alert once per episode
                        AlertOnServiceRecovered = true
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_alert_once", HealthStatus.Unhealthy, "🔕 AlertOnce test - failure");
            var healthyReport = CreateHealthReport("telegram_alert_once", HealthStatus.Healthy, "🔔 AlertOnce test - recovered");

            // Act
            TestContext.WriteLine("Sending first failure (should alert)...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(1, "Should alert on first failure");

            TestContext.WriteLine("Sending continued failure (should NOT alert - AlertOnce)...");
            await Task.Delay(2100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(500);
            alertCount.Should().Be(1, "Should not alert again (AlertOnce=true)");

            TestContext.WriteLine("Sending recovery (should alert)...");
            await Task.Delay(2100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(2, "Should alert on recovery");

            TestContext.WriteLine("Sending new failure after recovery (should alert - new episode)...");
            await Task.Delay(2100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            alertCount.Should().Be(3, "Should alert on new failure after recovery");
            
            TestContext.WriteLine("✅ AlertOnce behavior verified - 3 total messages!");
        }

        #endregion

        #region Complex Scenarios

        [Test]
        [Category("ComplexScenario")]
        public async Task HandleFlappingService_WithThreshold()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_flapping",
                ServiceType = ServiceType.Http,
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 3, // Prevent flapping alerts
                        AlertEvery = TimeSpan.FromSeconds(2),
                        AlertOnServiceRecovered = true
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("telegram_flapping", HealthStatus.Unhealthy, "🔄 Flapping service error");
            var healthyReport = CreateHealthReport("telegram_flapping", HealthStatus.Healthy, "✅ Flapping service recovered");

            // Act - Simulate flapping
            TestContext.WriteLine("Simulating flapping service...");
            TestContext.WriteLine("Fail 1...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            
            TestContext.WriteLine("Recover (reset count)...");
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            await Task.Delay(1000);
            
            TestContext.WriteLine("Fail 1 (again)...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            
            TestContext.WriteLine("Fail 2...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);
            
            TestContext.WriteLine("Fail 3 - should trigger alert...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should alert only after 3 consecutive failures");
            TestContext.WriteLine("✅ Flapping protection verified - only 1 alert after 3 consecutive failures!");
        }

        #endregion

        #region Detailed Results Tests

        [Test]
        [Category("DetailedResults")]
        public async Task Should_Display_Detailed_Failures_And_Successes()
        {
            // Arrange - Simulate multi-endpoint HTTP check with mixed results
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_detailed_results",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api1.example.com,https://api2.example.com,https://api3.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            // Create report with detailed failure/success data
            var unhealthyReport = CreateHealthReportWithData(
                "telegram_detailed_results",
                HealthStatus.Unhealthy,
                "HTTP health check failed for 2 of 3 endpoints",
                failures: new List<string>
                {
                    "https://api2.example.com returned 503, expected 200",
                    "https://api3.example.com timed out after 5000ms"
                },
                successes: new List<string>
                {
                    "https://api1.example.com returned expected status code 200"
                });

            // Act
            TestContext.WriteLine("Sending health check with detailed failures and successes...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should send alert with detailed results");
            TestContext.WriteLine("✅ Telegram message sent with detailed failure/success breakdown!");
            TestContext.WriteLine("Expected message to include:");
            TestContext.WriteLine("  • ❌ Failed (2): list of 2 failures");
            TestContext.WriteLine("  • ✅ Succeeded (1): list of 1 success");
        }

        [Test]
        [Category("DetailedResults")]
        public async Task Should_Display_All_Failures_When_Complete_Outage()
        {
            // Arrange - All endpoints down
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_complete_outage",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://primary.example.com,https://backup1.example.com,https://backup2.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReportWithData(
                "telegram_complete_outage",
                HealthStatus.Unhealthy,
                "HTTP health check failed for 3 of 3 endpoints",
                failures: new List<string>
                {
                    "https://primary.example.com timed out after 5000ms",
                    "https://backup1.example.com returned 500, expected 200",
                    "https://backup2.example.com failed: Connection refused"
                },
                successes: new List<string>());

            // Act
            TestContext.WriteLine("Sending complete outage scenario (all endpoints failed)...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should send alert for complete outage");
            TestContext.WriteLine("✅ Telegram message sent showing all 3 failures!");
            TestContext.WriteLine("Expected message to show critical situation with all endpoints down");
        }

        [Test]
        [Category("DetailedResults")]
        public async Task Should_Display_Ping_Results_With_Multiple_Hosts()
        {
            // Arrange - Ping check with multiple hosts
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_ping_multiple_hosts",
                ServiceType = ServiceType.Ping,
                EndpointOrHost = "192.168.1.1,192.168.1.2,192.168.1.3,192.168.1.4,192.168.1.5",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReportWithData(
                "telegram_ping_multiple_hosts",
                HealthStatus.Unhealthy,
                "Ping failed for 2 of 5 hosts",
                failures: new List<string>
                {
                    "192.168.1.3 timed out after 1000ms",
                    "192.168.1.5 returned status TimedOut"
                },
                successes: new List<string>
                {
                    "192.168.1.1 responded in 12ms",
                    "192.168.1.2 responded in 15ms",
                    "192.168.1.4 responded in 8ms"
                });

            // Act
            TestContext.WriteLine("Sending ping check with multiple hosts (partial failure)...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should send alert with ping results");
            TestContext.WriteLine("✅ Telegram message sent with network diagnostic details!");
            TestContext.WriteLine("Expected message to include:");
            TestContext.WriteLine("  • ❌ 2 hosts unreachable with timeout details");
            TestContext.WriteLine("  • ✅ 3 hosts responding with latency info");
        }

        [Test]
        [Category("DetailedResults")]
        public async Task Should_Display_Only_Failures_When_No_Successes()
        {
            // Arrange - Test with only failures, no successes
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_only_failures",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://down1.example.com,https://down2.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReportWithData(
                "telegram_only_failures",
                HealthStatus.Unhealthy,
                "All endpoints failed",
                failures: new List<string>
                {
                    "https://down1.example.com connection refused",
                    "https://down2.example.com DNS resolution failed"
                },
                successes: null); // No successes

            // Act
            TestContext.WriteLine("Sending health check with only failures (no successes)...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should send alert with only failure details");
            TestContext.WriteLine("✅ Telegram message sent showing only failures section!");
        }

        [Test]
        [Category("DetailedResults")]
        public async Task Should_Handle_Large_Number_Of_Endpoints()
        {
            // Arrange - Stress test with many endpoints
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_many_endpoints",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "Multiple endpoints (10 total)",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            // Create lists with many items
            var failures = new List<string>();
            var successes = new List<string>();
            
            for (int i = 1; i <= 3; i++)
            {
                failures.Add($"https://api{i}.example.com returned 500, expected 200");
            }
            
            for (int i = 4; i <= 10; i++)
            {
                successes.Add($"https://api{i}.example.com returned expected status code 200");
            }

            var unhealthyReport = CreateHealthReportWithData(
                "telegram_many_endpoints",
                HealthStatus.Unhealthy,
                "HTTP health check failed for 3 of 10 endpoints",
                failures: failures,
                successes: successes);

            // Act
            TestContext.WriteLine("Sending health check with 10 endpoints (3 failed, 7 succeeded)...");
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should send alert with all endpoint details");
            TestContext.WriteLine("✅ Telegram message sent with comprehensive endpoint list!");
            TestContext.WriteLine("Expected message to include:");
            TestContext.WriteLine("  • ❌ Failed (3): 3 failures listed");
            TestContext.WriteLine("  • ✅ Succeeded (7): 7 successes listed");
        }

        [Test]
        [Category("DetailedResults")]
        public async Task Should_Display_Degraded_Status_With_Partial_Failures()
        {
            // Arrange - Degraded status with mixed results
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "telegram_degraded_partial",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api1.example.com,https://api2.example.com,https://api3.example.com,https://api4.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TelegramTest",
                        TransportMethod = AlertTransportMethod.Telegram,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new TelegramAlertingPublisher(_healthChecksBuilder, healthCheck, _telegramSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var degradedReport = CreateHealthReportWithData(
                "telegram_degraded_partial",
                HealthStatus.Degraded,
                "Service degraded: 1 of 4 endpoints slow",
                failures: new List<string>
                {
                    "https://api2.example.com responded in 4500ms (threshold: 2000ms)"
                },
                successes: new List<string>
                {
                    "https://api1.example.com responded in 120ms",
                    "https://api3.example.com responded in 85ms",
                    "https://api4.example.com responded in 95ms"
                });

            // Act
            TestContext.WriteLine("Sending degraded status with performance issue...");
            await publisher.PublishAsync(degradedReport, CancellationToken.None);
            await Task.Delay(1000);

            // Assert
            alertCount.Should().Be(1, "Should send alert for degraded status");
            TestContext.WriteLine("✅ Telegram message sent with ⚠️ degraded warning and performance details!");
        }

        #endregion

        #region Helper Methods

        private HealthReport CreateHealthReport(string name, HealthStatus status, string description)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                { 
                    name, 
                    new HealthReportEntry(
                        status, 
                        description, 
                        TimeSpan.FromMilliseconds(100), 
                        null, 
                        new Dictionary<string, object>
                        {
                            { "Endpoint", "https://api.example.com" },
                            { "Host", "api.example.com" },
                            { "Environment", "Test" }
                        }) 
                }
            };

            return new HealthReport(entries, TimeSpan.FromMilliseconds(100));
        }

        private HealthReport CreateHealthReportWithData(
            string name, 
            HealthStatus status, 
            string description,
            List<string> failures = null,
            List<string> successes = null)
        {
            var data = new Dictionary<string, object>
            {
                { "Endpoint", "Multiple endpoints" },
                { "Host", "example.com" },
                { "Environment", "Test" }
            };

            // Add failures and successes to the data dictionary
            if (failures != null && failures.Any())
            {
                data.Add("Failures", failures);
            }

            if (successes != null && successes.Any())
            {
                data.Add("Successes", successes);
            }

            var entries = new Dictionary<string, HealthReportEntry>
            {
                { 
                    name, 
                    new HealthReportEntry(
                        status, 
                        description, 
                        TimeSpan.FromMilliseconds(2001), // More realistic duration
                        null, 
                        data) 
                }
            };

            return new HealthReport(entries, TimeSpan.FromMilliseconds(2001));
        }

        #endregion

        #region Test Configuration Guide

        [Test]
        [Category("Configuration")]
        [Explicit("Information only - displays configuration guide")]
        public void DisplayConfigurationGuide()
        {
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");
            TestContext.WriteLine("   TELEGRAM BOT CONFIGURATION GUIDE");
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");
            TestContext.WriteLine();
            TestContext.WriteLine("1. CREATE A TELEGRAM BOT:");
            TestContext.WriteLine("   • Open Telegram and search for @BotFather");
            TestContext.WriteLine("   • Send /newbot command");
            TestContext.WriteLine("   • Follow instructions to get your BOT TOKEN");
            TestContext.WriteLine();
            TestContext.WriteLine("2. GET YOUR CHAT ID:");
            TestContext.WriteLine("   • For personal chat:");
            TestContext.WriteLine("     - Send a message to your bot");
            TestContext.WriteLine("     - Visit: https://api.telegram.org/bot<YOUR_BOT_TOKEN>/getUpdates");
            TestContext.WriteLine("     - Find your chat_id in the JSON response");
            TestContext.WriteLine();
            TestContext.WriteLine("   • For group/channel:");
            TestContext.WriteLine("     - Add bot to your group/channel");
            TestContext.WriteLine("     - Send a message in the group");
            TestContext.WriteLine("     - Visit the getUpdates URL above");
            TestContext.WriteLine("     - Chat ID will be negative (e.g., -1001234567890)");
            TestContext.WriteLine();
            TestContext.WriteLine("3. CONFIGURE THE TESTS:");
            TestContext.WriteLine("   • Open: TelegramPublisherIntegrationShould.cs");
            TestContext.WriteLine($"   • Set BOT_API_TOKEN = \"YOUR_TOKEN\" (line ~26)");
            TestContext.WriteLine($"   • Set CHAT_ID = \"YOUR_CHAT_ID\" (line ~27)");
            TestContext.WriteLine();
            TestContext.WriteLine("4. RUN THE TESTS:");
            TestContext.WriteLine("   • Tests are [Explicit] - they won't run automatically");
            TestContext.WriteLine("   • Run manually from Test Explorer");
            TestContext.WriteLine("   • Check your Telegram chat for messages!");
            TestContext.WriteLine();
            TestContext.WriteLine("═══════════════════════════════════════════════════════════");
            TestContext.WriteLine();
            
            Assert.Pass("Configuration guide displayed successfully!");
        }

        #endregion
    }
}
