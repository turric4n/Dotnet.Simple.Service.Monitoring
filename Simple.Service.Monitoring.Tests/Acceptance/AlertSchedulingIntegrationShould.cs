using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Acceptance
{
    /// <summary>
    /// Integration tests for alert scheduling scenarios with time-based behaviors
    /// Tests cooldown periods, time windows, fail counts, and recovery scenarios
    /// </summary>
    [TestFixture(Category = "Acceptance")]
    [Category("AlertScheduling")]
    public class AlertSchedulingIntegrationShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private DictionaryTransportSettings _transportSettings;

        [SetUp]
        public void Setup()
        {
            var healthcheckbuildermock = new Mock<IHealthChecksBuilder>();
            healthcheckbuildermock
                .Setup(m => m.Services)
                .Returns(new ServiceCollection());
            _healthChecksBuilder = healthcheckbuildermock.Object;

            _transportSettings = new DictionaryTransportSettings()
            {
                Name = "TestTransport",
            };
        }

        #region Cooldown Period Tests

        [Test]
        [Category("Cooldown")]
        public async Task RespectCooldownPeriod_WhenContinuousFailures()
        {
            // Arrange
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "cooldown_test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.FromSeconds(3), // 3 second cooldown
                        AlertOnServiceRecovered = false,
                        AlertByFailCount = 1,
                        PublishAllResults = false
                    }
                },
                ServiceType = ServiceType.Http,
                EndpointOrHost = "http://test.com",
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("cooldown_test", HealthStatus.Unhealthy);

            // Act & Assert
            // First alert - should fire
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "First failure should trigger alert");

            // Immediate second check - should NOT fire (cooldown)
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should not alert during cooldown period");

            // Wait 1 second - still in cooldown
            await Task.Delay(1000);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should still be in cooldown after 1 second");

            // Wait 2 more seconds (total 3) - cooldown expired
            await Task.Delay(2000);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(2, "Should alert after cooldown period expires");
        }

        [Test]
        [Category("Cooldown")]
        public async Task IncreaseCooldownPeriod_OverMultipleIterations()
        {
            // Arrange - Simulating exponential backoff-like behavior
            var alertTimes = new List<DateTime>();
            
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "increasing_cooldown",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.FromSeconds(1), // Start with 1 second
                        AlertByFailCount = 1,
                        PublishAllResults = false
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertTimes.Add(DateTime.Now));

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("increasing_cooldown", HealthStatus.Unhealthy);

            // Act - Simulate increasing cooldown periods
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertTimes.Count.Should().Be(1);

            // Increase cooldown to 2 seconds
            healthCheck.AlertBehaviour[0].AlertEvery = TimeSpan.FromSeconds(2);
            await Task.Delay(1500);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertTimes.Count.Should().Be(1, "Should not alert with increased cooldown");

            await Task.Delay(600); // Total 2.1 seconds
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertTimes.Count.Should().Be(2, "Should alert after increased cooldown period");

            // Assert - Verify time differences
            var timeDiff = (alertTimes[1] - alertTimes[0]).TotalSeconds;
            timeDiff.Should().BeGreaterThan(2, "Time between alerts should respect increased cooldown");
        }

        #endregion

        #region Time Window Tests

        [Test]
        [Category("TimeWindow")]
        public async Task AlertOnlyDuringBusinessHours()
        {
            // Arrange
            var now = DateTime.Now;
            var businessHoursStart = now.TimeOfDay; // Current time
            var businessHoursEnd = now.AddHours(2).TimeOfDay; // 2 hours from now

            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "business_hours_test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        StartAlertingOn = businessHoursStart,
                        StopAlertingOn = businessHoursEnd,
                        AlertEvery = TimeSpan.FromSeconds(1),
                        AlertByFailCount = 1
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("business_hours_test", HealthStatus.Unhealthy);

            // Act - Should alert (within business hours)
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should alert during business hours");

            // Change window to be outside current time
            healthCheck.AlertBehaviour[0].StartAlertingOn = now.AddHours(3).TimeOfDay;
            healthCheck.AlertBehaviour[0].StopAlertingOn = now.AddHours(5).TimeOfDay;

            await Task.Delay(1100); // Wait for cooldown
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should NOT alert outside business hours");
        }

        [Test]
        [Category("TimeWindow")]
        [Category("MidnightCrossing")]
        public async Task AlertDuringNightShiftWindow()
        {
            // Arrange - Night shift: 22:00 - 02:00
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "night_shift_test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        StartAlertingOn = TimeSpan.Parse("22:00:00"),
                        StopAlertingOn = TimeSpan.Parse("02:00:00"),
                        AlertEvery = TimeSpan.FromSeconds(1),
                        AlertByFailCount = 1
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);

            // Test if current time is within night shift window
            var currentTime = DateTime.Now.TimeOfDay;
            var inNightShift = publisher.TimeBetweenScheduler(
                TimeSpan.Parse("22:00:00"),
                TimeSpan.Parse("02:00:00"),
                currentTime);

            // Assert
            if (currentTime >= TimeSpan.Parse("22:00:00") || currentTime < TimeSpan.Parse("02:00:00"))
            {
                inNightShift.Should().BeTrue("Current time should be within night shift window");
            }
            else
            {
                inNightShift.Should().BeFalse("Current time should be outside night shift window");
            }
        }

        #endregion

        #region Fail Count Threshold Tests

        [Test]
        [Category("FailCount")]
        public async Task AlertOnlyAfterConsecutiveFailures()
        {
            // Arrange
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "fail_count_test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertByFailCount = 3, // Alert after 3 consecutive failures
                        AlertEvery = TimeSpan.FromSeconds(1),
                        PublishAllResults = false
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("fail_count_test", HealthStatus.Unhealthy);
            var healthyReport = CreateHealthReport("fail_count_test", HealthStatus.Healthy);

            // Act & Assert
            // Failure 1
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(0, "Should not alert on first failure");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);

            // Failure 2
            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(0, "Should not alert on second failure");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(2);

            // Failure 3 - should trigger alert
            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should alert on third consecutive failure");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(3);

            // Recovery - should reset count
            await Task.Delay(1100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(0, "Failed count should reset on recovery");

            // Single failure after recovery - should not alert
            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should not alert on single failure after recovery");
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        [Category("FailCount")]
        public async Task IncreaseFailCountThreshold_OverTime()
        {
            // Arrange - Simulating progressive alerting (more lenient over time)
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "progressive_fail_count",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertByFailCount = 2, // Start with 2 failures
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("progressive_fail_count", HealthStatus.Unhealthy);

            // Act - First round with threshold of 2
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should alert after 2 failures");

            // Increase threshold to 5
            healthCheck.AlertBehaviour[0].AlertByFailCount = 5;
            healthCheck.AlertBehaviour[0].FailedCount = 0; // Reset for test

            // Need 5 failures now
            for (int i = 0; i < 4; i++)
            {
                await Task.Delay(1100);
                await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            }
            alertCount.Should().Be(1, "Should not alert before reaching new threshold");

            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(2, "Should alert after reaching increased threshold");
        }

        #endregion

        #region Recovery Alert Tests

        [Test]
        [Category("Recovery")]
        public async Task AlertOnServiceRecovery_WhenConfigured()
        {
            // Arrange
            var alertCount = 0;
            var lastAlertWasRecovery = false;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "recovery_test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertOnServiceRecovered = true,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback<KeyValuePair<string, HealthReportEntry>>(entry =>
                {
                    alertCount++;
                    lastAlertWasRecovery = entry.Value.Status == HealthStatus.Healthy;
                });

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("recovery_test", HealthStatus.Unhealthy);
            var healthyReport = CreateHealthReport("recovery_test", HealthStatus.Healthy);

            // Act & Assert
            // Failure
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should alert on failure");
            lastAlertWasRecovery.Should().BeFalse("Alert should be for failure");

            // Recovery
            await Task.Delay(1100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            alertCount.Should().Be(2, "Should alert on recovery");
            lastAlertWasRecovery.Should().BeTrue("Alert should be for recovery");
        }

        [Test]
        [Category("Recovery")]
        public async Task NotAlertOnRecovery_WhenNotConfigured()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "no_recovery_alert",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertOnServiceRecovered = false, // Don't alert on recovery
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("no_recovery_alert", HealthStatus.Unhealthy);
            var healthyReport = CreateHealthReport("no_recovery_alert", HealthStatus.Healthy);

            // Act & Assert
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should alert on failure");

            await Task.Delay(1100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should NOT alert on recovery when disabled");
        }

        #endregion

        #region Alert Once Tests

        [Test]
        [Category("AlertOnce")]
        public async Task AlertOnce_ThenSilenceUntilRecovery()
        {
            // Arrange
            var alertCount = 0;

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "alert_once_test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertOnce = true, // Only alert once per failure episode
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1),
                        AlertOnServiceRecovered = true
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("alert_once_test", HealthStatus.Unhealthy);
            var healthyReport = CreateHealthReport("alert_once_test", HealthStatus.Healthy);

            // Act & Assert
            // First failure - should alert
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should alert on first failure");

            // Continue failing - should NOT alert again
            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should not alert again (AlertOnce=true)");

            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(1, "Should still not alert");

            // Recovery - should alert
            await Task.Delay(1100);
            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            alertCount.Should().Be(2, "Should alert on recovery");

            // New failure - should alert again
            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            alertCount.Should().Be(3, "Should alert on new failure after recovery");
        }

        #endregion

        #region Complex Scenario Tests

        [Test]
        [Category("ComplexScenario")]
        public async Task ComplexScenario_FlappingService()
        {
            // Arrange - Service that flaps between healthy and unhealthy
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "flapping_service",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertByFailCount = 2, // Require 2 consecutive failures
                        AlertEvery = TimeSpan.FromSeconds(2),
                        AlertOnServiceRecovered = true
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("flapping_service", HealthStatus.Unhealthy);
            var healthyReport = CreateHealthReport("flapping_service", HealthStatus.Healthy);

            // Act - Simulate flapping
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None); // Fail 1
            await Task.Delay(500);
            
            await publisher.PublishAsync(healthyReport, CancellationToken.None); // Recover (count reset)
            alertCount.Should().Be(0, "Should not alert on single failure");
            
            await Task.Delay(500);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None); // Fail 1 (again)
            await Task.Delay(500);
            
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None); // Fail 2 - ALERT
            alertCount.Should().Be(1, "Should alert after 2 consecutive failures");
            
            await Task.Delay(500);
            await publisher.PublishAsync(healthyReport, CancellationToken.None); // Recover - ALERT
            alertCount.Should().Be(2, "Should alert on recovery");
        }

        [Test]
        [Category("ComplexScenario")]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(20)]
        public async Task StressTest_RapidStatusChanges(int iterations)
        {
            // Arrange
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "stress_test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromMilliseconds(100),
                        AlertOnServiceRecovered = true
                    }
                },
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _transportSettings);
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => Interlocked.Increment(ref alertCount));

            publisher.Subscribe(observer.Object);

            var random = new Random();
            var unhealthyReport = CreateHealthReport("stress_test", HealthStatus.Unhealthy);
            var healthyReport = CreateHealthReport("stress_test", HealthStatus.Healthy);

            // Act - Rapid random status changes
            for (int i = 0; i < iterations; i++)
            {
                var report = random.Next(2) == 0 ? unhealthyReport : healthyReport;
                await publisher.PublishAsync(report, CancellationToken.None);
                await Task.Delay(random.Next(50, 200));
            }

            // Assert
            alertCount.Should().BeGreaterThan(0, "Should have triggered some alerts");
            alertCount.Should().BeLessThan(iterations, "Should not alert on every check due to cooldown");
        }

        #endregion

        #region Helper Methods

        private HealthReport CreateHealthReport(string name, HealthStatus status)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                { name, new HealthReportEntry(status, $"Test {status}", TimeSpan.FromMilliseconds(50), null, null) }
            };

            return new HealthReport(entries, TimeSpan.FromMilliseconds(50));
        }

        #endregion
    }
}
