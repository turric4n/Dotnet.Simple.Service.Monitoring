using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.InfluxDB;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Publishers
{
    [TestFixture]
    [Category("Unit")]
    [Category("InfluxDbPublisher")]
    public class InfluxDbPublisherShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private InfluxDbTransportSettings _influxDbSettings;

        [SetUp]
        public void Setup()
        {
            var healthcheckbuildermock = new Mock<IHealthChecksBuilder>();
            healthcheckbuildermock
                .Setup(m => m.Services)
                .Returns(new ServiceCollection());
            _healthChecksBuilder = healthcheckbuildermock.Object;

            _influxDbSettings = new InfluxDbTransportSettings()
            {
                Name = "InfluxDbTest",
                Host = "http://localhost:8086",
                Database = "test_monitoring"
            };
        }

        [Test]
        public void PublishAsync_Should_NotThrow_WithValidConfiguration()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "influx_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "InfluxDbTest",
                        TransportMethod = AlertTransportMethod.Influx,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromMinutes(5)
                    }
                },
                Alert = true
            };

            var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, _influxDbSettings);
            var report = CreateHealthReport("influx_test", HealthStatus.Unhealthy, "Service unavailable");

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public void Should_PublishOnFirstFailure_WhenAlertByFailCountIsOne()
        {
            // Arrange
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "influx_first_failure",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "InfluxDbTest",
                        TransportMethod = AlertTransportMethod.Influx,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromMinutes(5)
                    }
                },
                Alert = true
            };

            var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, _influxDbSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("influx_first_failure", HealthStatus.Unhealthy, "Service unavailable");

            // Act
            publisher.PublishAsync(unhealthyReport, CancellationToken.None).Wait();
            Thread.Sleep(100);

            // Assert
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);
        }

        [Test]
        public async Task Should_RespectAlertByFailCount_Threshold()
        {
            // Arrange
            var alertCount = 0;
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "influx_threshold_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "InfluxDbTest",
                        TransportMethod = AlertTransportMethod.Influx,
                        AlertByFailCount = 3,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, _influxDbSettings);
            
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => alertCount++);

            publisher.Subscribe(observer.Object);

            var unhealthyReport = CreateHealthReport("influx_threshold_test", HealthStatus.Unhealthy, "Connection timeout");

            // Act
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(100);
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);

            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(100);
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(2);

            await Task.Delay(1100);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(100);
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(3);
        }

        [Test]
        public async Task Should_ResetFailureCount_WhenServiceRecovers()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "influx_recovery_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "InfluxDbTest",
                        TransportMethod = AlertTransportMethod.Influx,
                        AlertByFailCount = 2,
                        AlertEvery = TimeSpan.FromSeconds(1),
                        AlertOnServiceRecovered = true
                    }
                },
                Alert = true
            };

            var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, _influxDbSettings);
            
            var unhealthyReport = CreateHealthReport("influx_recovery_test", HealthStatus.Unhealthy, "Service down");
            var healthyReport = CreateHealthReport("influx_recovery_test", HealthStatus.Healthy, "Service recovered");

            // Act
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(100);
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1);

            await publisher.PublishAsync(healthyReport, CancellationToken.None);
            await Task.Delay(100);

            // Assert
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(0, "Failure count should reset on recovery");
        }

        [Test]
        public async Task Should_HandleDegradedStatus()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "influx_degraded_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "InfluxDbTest",
                        TransportMethod = AlertTransportMethod.Influx,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(1)
                    }
                },
                Alert = true
            };

            var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, _influxDbSettings);
            var degradedReport = CreateHealthReport("influx_degraded_test", HealthStatus.Degraded, "Slow response time");

            // Act
            await publisher.PublishAsync(degradedReport, CancellationToken.None);
            await Task.Delay(100);

            // Assert
            healthCheck.AlertBehaviour[0].FailedCount.Should().Be(1, "Degraded status should count as failure");
        }

        [Test]
        public async Task Should_RespectAlertEvery_Interval()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "influx_interval_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "InfluxDbTest",
                        TransportMethod = AlertTransportMethod.Influx,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromSeconds(2)
                    }
                },
                Alert = true
            };

            var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, _influxDbSettings);
            var unhealthyReport = CreateHealthReport("influx_interval_test", HealthStatus.Unhealthy, "Service unavailable");

            // Act - First alert should work
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(100);
            var firstAlertTime = healthCheck.AlertBehaviour[0].LastPublished;
            firstAlertTime.Should().NotBe(default(DateTime), "First alert should be sent");

            // Act - Second alert within interval should be ignored
            await Task.Delay(500);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(100);
            healthCheck.AlertBehaviour[0].LastPublished.Should().Be(firstAlertTime, "Should not alert within interval");

            // Act - Third alert after interval should work
            await Task.Delay(2000);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await Task.Delay(100);
            healthCheck.AlertBehaviour[0].LastPublished.Should().BeAfter(firstAlertTime, "Should alert after interval");
        }

        [Test]
        public void Should_IncludeHealthCheckData_InInfluxMetrics()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "influx_data_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "InfluxDbTest",
                        TransportMethod = AlertTransportMethod.Influx,
                        AlertByFailCount = 1,
                        AlertEvery = TimeSpan.FromMinutes(5)
                    }
                },
                Alert = true
            };

            var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, _influxDbSettings);
            
            var customData = new Dictionary<string, object>
            {
                { "custom_metric", 42 },
                { "region", "us-west-2" }
            };

            var report = CreateHealthReportWithData("influx_data_test", HealthStatus.Unhealthy, "Test", customData);

            // Act & Assert - Should not throw
            Assert.DoesNotThrowAsync(async () => await publisher.PublishAsync(report, CancellationToken.None));
        }

        [Test]
        public void Validate_Should_ThrowException_When_HostIsNull()
        {
            // Arrange
            var invalidSettings = new InfluxDbTransportSettings()
            {
                Name = "InvalidTest",
                Host = null,
                Database = "test_db"
            };

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "validation_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                Alert = true
            };

            // Act & Assert
            Assert.Throws<InfluxDbValidationError>(() =>
            {
                var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        [Test]
        public void Validate_Should_ThrowException_When_DatabaseIsNull()
        {
            // Arrange
            var invalidSettings = new InfluxDbTransportSettings()
            {
                Name = "InvalidTest",
                Host = "http://localhost:8086",
                Database = null
            };

            var healthCheck = new ServiceHealthCheck()
            {
                Name = "validation_test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = "https://api.example.com",
                Alert = true
            };

            // Act & Assert
            Assert.Throws<InfluxDbValidationError>(() =>
            {
                var publisher = new InfluxDbAlertingPublisher(_healthChecksBuilder, healthCheck, invalidSettings);
                publisher.SetUp();
            });
        }

        #region Helper Methods

        private HealthReport CreateHealthReport(string name, HealthStatus status, string description)
        {
            var entry = new HealthReportEntry(
                status: status,
                description: description,
                duration: TimeSpan.FromMilliseconds(100),
                exception: null,
                data: null);

            var entries = new Dictionary<string, HealthReportEntry>
            {
                { name, entry }
            };

            return new HealthReport(entries, TimeSpan.FromMilliseconds(100));
        }

        private HealthReport CreateHealthReportWithData(string name, HealthStatus status, string description, IReadOnlyDictionary<string, object> data)
        {
            var entry = new HealthReportEntry(
                status: status,
                description: description,
                duration: TimeSpan.FromMilliseconds(100),
                exception: null,
                data: data);

            var entries = new Dictionary<string, HealthReportEntry>
            {
                { name, entry }
            };

            return new HealthReport(entries, TimeSpan.FromMilliseconds(100));
        }

        #endregion
    }
}

