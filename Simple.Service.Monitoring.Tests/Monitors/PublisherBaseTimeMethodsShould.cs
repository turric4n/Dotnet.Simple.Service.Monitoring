using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using System;
using System.Collections.Generic;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    /// <summary>
    /// Comprehensive tests for time-related methods in PublisherBase
    /// Tests cover normal scenarios, edge cases, and midnight rollover bugs
    /// </summary>
    [TestFixture(Category = "Unit")]
    public class PublisherBaseTimeMethodsShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private DictionaryTransportSettings _alertTransportSettings;

        [SetUp]
        public void Setup()
        {
            var healthcheckbuildermock = new Moq.Mock<IHealthChecksBuilder>();
            healthcheckbuildermock
                .Setup(m => m.Services)
                .Returns(new ServiceCollection());
            _healthChecksBuilder = healthcheckbuildermock.Object;

            _alertTransportSettings = new DictionaryTransportSettings()
            {
                Name = "TestTransport",
            };
        }

        private DictionaryPublisher CreatePublisher(ServiceHealthCheck healthCheck)
        {
            return new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);
        }

        #region TimeBetweenIsOkToAlert Tests

        [Test]
        [Category("TimeBetweenIsOkToAlert")]
        [TestCase("08:00:00", "00:05:00", "08:05:00", ExpectedResult = true, Description = "Exactly at cooldown boundary")]
        [TestCase("08:00:00", "00:05:00", "08:06:00", ExpectedResult = true, Description = "After cooldown period")]
        [TestCase("08:00:00", "00:05:00", "08:04:59", ExpectedResult = false, Description = "Just before cooldown")]
        [TestCase("14:30:00", "00:15:00", "14:45:00", ExpectedResult = true, Description = "15 min cooldown exact")]
        [TestCase("14:30:00", "00:15:00", "14:50:00", ExpectedResult = true, Description = "15 min cooldown exceeded")]
        [TestCase("14:30:00", "00:15:00", "14:40:00", ExpectedResult = false, Description = "Within cooldown period")]
        public bool TimeBetweenIsOkToAlert_NormalScenarios(TimeSpan lastAlert, TimeSpan cooldown, TimeSpan current)
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            return publisher.TimeBetweenIsOkToAlert(lastAlert, cooldown, current);
        }

        [Test]
        [Category("TimeBetweenIsOkToAlert")]
        [Category("MidnightRollover")]
        [TestCase("23:55:00", "00:10:00", "00:03:00", ExpectedResult = false, Description = "Midnight rollover - 8 minutes passed, need 10")]
        [TestCase("23:55:00", "00:10:00", "00:05:00", ExpectedResult = true, Description = "Midnight rollover - exact boundary")]
        [TestCase("23:55:00", "00:10:00", "00:06:00", ExpectedResult = true, Description = "Midnight rollover - 11 minutes passed")]
        [TestCase("23:50:00", "00:15:00", "00:04:00", ExpectedResult = false, Description = "Midnight rollover - within cooldown")]
        [TestCase("23:50:00", "00:15:00", "00:05:00", ExpectedResult = true, Description = "Midnight rollover - exactly 15 min")]
        [TestCase("23:45:00", "00:30:00", "00:10:00", ExpectedResult = false, Description = "Midnight rollover - long cooldown not met")]
        [TestCase("23:45:00", "00:30:00", "00:15:00", ExpectedResult = true, Description = "Midnight rollover - long cooldown met")]
        public bool TimeBetweenIsOkToAlert_MidnightRollover(TimeSpan lastAlert, TimeSpan cooldown, TimeSpan current)
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            return publisher.TimeBetweenIsOkToAlert(lastAlert, cooldown, current);
        }

        [Test]
        [Category("TimeBetweenIsOkToAlert")]
        [Category("EdgeCases")]
        [TestCase("00:00:00", "00:05:00", "00:05:00", ExpectedResult = true, Description = "Start of day")]
        [TestCase("23:59:00", "00:01:00", "00:00:00", ExpectedResult = true, Description = "End of day rollover")]
        [TestCase("12:00:00", "12:00:00", "00:00:00", ExpectedResult = true, Description = "12 hour cooldown crosses midnight")]
        [TestCase("06:00:00", "18:00:00", "00:00:00", ExpectedResult = true, Description = "18 hour cooldown")]
        [TestCase("23:30:00", "01:00:00", "00:15:00", ExpectedResult = false, Description = "1 hour cooldown not met after midnight")]
        [TestCase("23:30:00", "01:00:00", "00:30:00", ExpectedResult = true, Description = "1 hour cooldown met after midnight")]
        public bool TimeBetweenIsOkToAlert_EdgeCases(TimeSpan lastAlert, TimeSpan cooldown, TimeSpan current)
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            return publisher.TimeBetweenIsOkToAlert(lastAlert, cooldown, current);
        }

        #endregion

        #region TimeBetweenScheduler Tests

        [Test]
        [Category("TimeBetweenScheduler")]
        [TestCase("08:00:00", "17:00:00", "10:00:00", ExpectedResult = true, Description = "Business hours - mid day")]
        [TestCase("08:00:00", "17:00:00", "08:00:00", ExpectedResult = true, Description = "Business hours - start boundary")]
        [TestCase("08:00:00", "17:00:00", "16:59:59", ExpectedResult = true, Description = "Business hours - just before end")]
        [TestCase("08:00:00", "17:00:00", "17:00:00", ExpectedResult = false, Description = "Business hours - at end (exclusive)")]
        [TestCase("08:00:00", "17:00:00", "07:59:59", ExpectedResult = false, Description = "Business hours - before start")]
        [TestCase("08:00:00", "17:00:00", "18:00:00", ExpectedResult = false, Description = "Business hours - after end")]
        [TestCase("00:00:00", "23:59:59", "12:00:00", ExpectedResult = true, Description = "24/7 alerting")]
        [TestCase("09:00:00", "09:00:00", "09:00:00", ExpectedResult = true, Description = "Zero duration window")]
        public bool TimeBetweenScheduler_NormalWindows(TimeSpan start, TimeSpan end, TimeSpan current)
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            return publisher.TimeBetweenScheduler(start, end, current);
        }

        [Test]
        [Category("TimeBetweenScheduler")]
        [Category("MidnightCrossing")]
        [TestCase("22:00:00", "02:00:00", "23:00:00", ExpectedResult = true, Description = "Night window - before midnight")]
        [TestCase("22:00:00", "02:00:00", "00:30:00", ExpectedResult = true, Description = "Night window - after midnight")]
        [TestCase("22:00:00", "02:00:00", "22:00:00", ExpectedResult = true, Description = "Night window - start boundary")]
        [TestCase("22:00:00", "02:00:00", "01:59:59", ExpectedResult = true, Description = "Night window - just before end")]
        [TestCase("22:00:00", "02:00:00", "02:00:00", ExpectedResult = false, Description = "Night window - at end (exclusive)")]
        [TestCase("22:00:00", "02:00:00", "03:00:00", ExpectedResult = false, Description = "Night window - after end")]
        [TestCase("22:00:00", "02:00:00", "21:00:00", ExpectedResult = false, Description = "Night window - before start")]
        [TestCase("22:00:00", "02:00:00", "05:00:00", ExpectedResult = false, Description = "Night window - middle of day")]
        [TestCase("20:00:00", "06:00:00", "23:00:00", ExpectedResult = true, Description = "Long night window - evening")]
        [TestCase("20:00:00", "06:00:00", "03:00:00", ExpectedResult = true, Description = "Long night window - early morning")]
        [TestCase("20:00:00", "06:00:00", "12:00:00", ExpectedResult = false, Description = "Long night window - midday")]
        [TestCase("23:00:00", "01:00:00", "00:00:00", ExpectedResult = true, Description = "Short night window - exact midnight")]
        public bool TimeBetweenScheduler_MidnightCrossingWindows(TimeSpan start, TimeSpan end, TimeSpan current)
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            return publisher.TimeBetweenScheduler(start, end, current);
        }

        [Test]
        [Category("TimeBetweenScheduler")]
        [Category("EdgeCases")]
        [TestCase("00:00:00", "00:00:01", "00:00:00", ExpectedResult = true, Description = "Very short window at midnight")]
        [TestCase("23:59:59", "00:00:01", "00:00:00", ExpectedResult = true, Description = "Window crossing midnight by 2 seconds")]
        [TestCase("23:59:59", "00:00:00", "23:59:59", ExpectedResult = true, Description = "Midnight boundary exclusive")]
        public bool TimeBetweenScheduler_EdgeCases(TimeSpan start, TimeSpan end, TimeSpan current)
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            return publisher.TimeBetweenScheduler(start, end, current);
        }

        #endregion

        #region HealthFailed Tests

        [Test]
        [Category("HealthFailed")]
        public void HealthFailed_ShouldReturnTrue_WhenUnhealthy()
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.HealthFailed(HealthStatus.Unhealthy);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        [Category("HealthFailed")]
        public void HealthFailed_ShouldReturnTrue_WhenDegraded()
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.HealthFailed(HealthStatus.Degraded);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        [Category("HealthFailed")]
        public void HealthFailed_ShouldReturnFalse_WhenHealthy()
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.HealthFailed(HealthStatus.Healthy);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region GetReportLastCheck Tests

        [Test]
        [Category("GetReportLastCheck")]
        public void GetReportLastCheck_ShouldReturnMinValue_WhenBehaviourIsNull()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "test",
                AlertBehaviour = new List<AlertBehaviour>() // Empty list
            };
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.GetReportLastCheck();

            // Assert
            result.Should().Be(DateTime.MinValue);
        }

        [Test]
        [Category("GetReportLastCheck")]
        public void GetReportLastCheck_ShouldReturnLastCheck_WhenNoTimezoneConfigured()
        {
            // Arrange
            var expectedTime = new DateTime(2024, 1, 15, 10, 30, 0);
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        LastCheck = expectedTime,
                        Timezone = null
                    }
                }
            };
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.GetReportLastCheck();

            // Assert
            result.Should().Be(expectedTime);
        }

        [Test]
        [Category("GetReportLastCheck")]
        public void GetReportLastCheck_ShouldReturnLastCheck_WhenTimezoneIsEmpty()
        {
            // Arrange
            var expectedTime = new DateTime(2024, 1, 15, 10, 30, 0);
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        LastCheck = expectedTime,
                        Timezone = ""
                    }
                }
            };
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.GetReportLastCheck();

            // Assert
            result.Should().Be(expectedTime);
        }

        [Test]
        [Category("GetReportLastCheck")]
        public void GetReportLastCheck_ShouldConvertTimezone_WhenValidTimezoneProvided()
        {
            // Arrange
            var utcTime = new DateTime(2024, 1, 15, 18, 0, 0, DateTimeKind.Utc);
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        LastCheck = utcTime,
                        Timezone = "America/New_York" // UTC-5
                    }
                }
            };
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.GetReportLastCheck();

            // Assert
            // Result should be converted to EST (UTC-5)
            result.Should().NotBe(utcTime);
            // The conversion should reflect the timezone offset
            result.Hour.Should().BeLessThan(utcTime.Hour); // Should be earlier in the day
        }

        [Test]
        [Category("GetReportLastCheck")]
        public void GetReportLastCheck_ShouldReturnOriginal_WhenInvalidTimezone()
        {
            // Arrange
            var expectedTime = new DateTime(2024, 1, 15, 10, 30, 0);
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "test",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        LastCheck = expectedTime,
                        Timezone = "Invalid/Timezone"
                    }
                }
            };
            var publisher = CreatePublisher(healthCheck);

            // Act
            var result = publisher.GetReportLastCheck();

            // Assert
            result.Should().Be(expectedTime);
        }

        #endregion

        #region Integration Tests - Combined Time Logic

        [Test]
        [Category("Integration")]
        [Category("MidnightScenarios")]
        public void AlertingWindow_And_Cooldown_ShouldBothWork_AcrossMidnight()
        {
            // Arrange - Night shift alerting (22:00 - 02:00) with 30 min cooldown
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "nightshift",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.FromMinutes(30),
                        StartAlertingOn = TimeSpan.Parse("22:00:00"),
                        StopAlertingOn = TimeSpan.Parse("02:00:00"),
                        LastPublished = new DateTime(2024, 1, 15, 23, 30, 0)
                    }
                }
            };
            var publisher = CreatePublisher(healthCheck);

            // Act & Assert - Multiple scenarios
            // 23:45 - Within window, cooldown not met (15 min since last alert)
            var inWindow1 = publisher.TimeBetweenScheduler(
                TimeSpan.Parse("22:00:00"),
                TimeSpan.Parse("02:00:00"),
                TimeSpan.Parse("23:45:00"));
            var cooldownOk1 = publisher.TimeBetweenIsOkToAlert(
                TimeSpan.Parse("23:30:00"),
                TimeSpan.FromMinutes(30),
                TimeSpan.Parse("23:45:00"));

            inWindow1.Should().BeTrue("23:45 is within night shift window");
            cooldownOk1.Should().BeFalse("Only 15 minutes passed, need 30");

            // 00:15 - Within window (after midnight), cooldown met (45 min since 23:30)
            var inWindow2 = publisher.TimeBetweenScheduler(
                TimeSpan.Parse("22:00:00"),
                TimeSpan.Parse("02:00:00"),
                TimeSpan.Parse("00:15:00"));
            var cooldownOk2 = publisher.TimeBetweenIsOkToAlert(
                TimeSpan.Parse("23:30:00"),
                TimeSpan.FromMinutes(30),
                TimeSpan.Parse("00:15:00"));

            inWindow2.Should().BeTrue("00:15 is within night shift window");
            cooldownOk2.Should().BeTrue("45 minutes passed since 23:30");

            // 03:00 - Outside window, cooldown met
            var inWindow3 = publisher.TimeBetweenScheduler(
                TimeSpan.Parse("22:00:00"),
                TimeSpan.Parse("02:00:00"),
                TimeSpan.Parse("03:00:00"));

            inWindow3.Should().BeFalse("03:00 is outside night shift window");
        }

        #endregion

        #region Helper Methods

        private ServiceHealthCheck CreateDefaultHealthCheck()
        {
            return new ServiceHealthCheck()
            {
                Name = "defaultcheck",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpVerb = HttpVerb.Get
                    }
                },
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        AlertEvery = TimeSpan.FromMinutes(5),
                        AlertOnServiceRecovered = true,
                        TransportName = "TestTransport",
                        TransportMethod = AlertTransportMethod.Email
                    }
                },
                EndpointOrHost = "https://example.com",
                ServiceType = ServiceType.Http,
                Alert = true
            };
        }

        #endregion
    }
}
