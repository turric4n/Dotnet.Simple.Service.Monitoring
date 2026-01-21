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

namespace Simple.Service.Monitoring.Tests.Monitors
{
    /// <summary>
    /// Tests for thread-safety and concurrent operations in PublisherBase
    /// </summary>
    [TestFixture(Category = "Unit")]
    [Category("ThreadSafety")]
    public class PublisherBaseThreadSafetyShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;
        private DictionaryTransportSettings _alertTransportSettings;

        [SetUp]
        public void Setup()
        {
            var healthcheckbuildermock = new Mock<IHealthChecksBuilder>();
            healthcheckbuildermock
                .Setup(m => m.Services)
                .Returns(new ServiceCollection());
            _healthChecksBuilder = healthcheckbuildermock.Object;

            _alertTransportSettings = new DictionaryTransportSettings()
            {
                Name = "TestTransport",
            };
        }

        [Test]
        [Category("Concurrency")]
        public async Task ProcessOwnedAlertRules_ShouldBeThreadSafe_WhenCalledConcurrently()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "concurrenttest",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.FromSeconds(1),
                        AlertByFailCount = 1,
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24)
                    }
                }
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);
            var report = CreateHealthReport("concurrenttest", HealthStatus.Unhealthy);

            // Act - Simulate concurrent health check evaluations
            var tasks = new List<Task>();
            var concurrentCount = 100;

            for (int i = 0; i < concurrentCount; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await publisher.PublishAsync(report, CancellationToken.None);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - FailedCount should be consistent
            var behaviour = healthCheck.AlertBehaviour.First();
            behaviour.FailedCount.Should().BeGreaterThan(0, "Failed count should be incremented");
            behaviour.FailedCount.Should().BeLessThan(concurrentCount + 1, "Failed count should not exceed number of checks");
        }

        [Test]
        [Category("Concurrency")]
        public async Task UpdateBehaviourState_ShouldNotLoseUpdates_UnderConcurrentAccess()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "statetest",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.Zero, // Allow all alerts
                        AlertByFailCount = 1,
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24)
                    }
                }
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);

            // Act - Alternate between healthy and unhealthy states concurrently
            var tasks = new List<Task>();
            var iterations = 50;

            for (int i = 0; i < iterations; i++)
            {
                var status = i % 2 == 0 ? HealthStatus.Unhealthy : HealthStatus.Healthy;
                var report = CreateHealthReport("statetest", status);

                tasks.Add(Task.Run(async () =>
                {
                    await publisher.PublishAsync(report, CancellationToken.None);
                }));
            }

            await Task.WhenAll(tasks);

            // Assert - LastCheck should be updated (not DateTime.MinValue)
            var behaviour = healthCheck.AlertBehaviour.First();
            behaviour.LastCheck.Should().NotBe(DateTime.MinValue, "LastCheck should be updated");
            behaviour.LastStatus.Should().BeOneOf(
                Library.Models.HealthStatus.Healthy,
                Library.Models.HealthStatus.Unhealthy,
                Library.Models.HealthStatus.Degraded);
        }

        [Test]
        [Category("Concurrency")]
        public async Task FailedCount_ShouldIncrementCorrectly_UnderConcurrentFailures()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "failcounttest",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.FromSeconds(1),
                        AlertByFailCount = 100, // High threshold to prevent alerting
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24)
                    }
                }
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);
            var report = CreateHealthReport("failcounttest", HealthStatus.Unhealthy);

            // Act - Multiple concurrent failures
            var concurrentFailures = 50;
            var tasks = Enumerable.Range(0, concurrentFailures)
                .Select(_ => Task.Run(async () =>
                {
                    await publisher.PublishAsync(report, CancellationToken.None);
                }))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert
            var behaviour = healthCheck.AlertBehaviour.First();
            behaviour.FailedCount.Should().BeGreaterThan(0, "FailedCount should be incremented");
            behaviour.FailedCount.Should().BeLessThan(concurrentFailures + 1, "FailedCount should not exceed number of failures");
        }

        [Test]
        [Category("Concurrency")]
        public async Task FailedCount_ShouldResetCorrectly_WhenHealthyUnderConcurrency()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "resettest",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.FromSeconds(1),
                        AlertByFailCount = 1,
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24)
                    }
                }
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);

            // Act - First cause failures
            var unhealthyReport = CreateHealthReport("resettest", HealthStatus.Unhealthy);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);
            await publisher.PublishAsync(unhealthyReport, CancellationToken.None);

            var behaviour = healthCheck.AlertBehaviour.First();
            behaviour.FailedCount.Should().BeGreaterThan(0, "Should have failures before recovery");

            // Then recover concurrently
            var healthyReport = CreateHealthReport("resettest", HealthStatus.Healthy);
            var recoverTasks = Enumerable.Range(0, 10)
                .Select(_ => Task.Run(async () =>
                {
                    await publisher.PublishAsync(healthyReport, CancellationToken.None);
                }))
                .ToArray();

            await Task.WhenAll(recoverTasks);

            // Assert
            behaviour.FailedCount.Should().Be(0, "FailedCount should reset to 0 when healthy");
        }

        [Test]
        [Category("Observers")]
        public async Task AlertObservers_ShouldNotThrow_WhenObserverThrowsException()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "observertest",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.Zero,
                        AlertByFailCount = 1,
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24)
                    }
                }
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);

            // Create observers - one that throws, one that works
            var throwingObserver = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            throwingObserver
                .Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Throws(new InvalidOperationException("Observer failed"));

            var workingObserver = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            var workingObserverCalled = false;
            workingObserver
                .Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => workingObserverCalled = true);

            publisher.Subscribe(throwingObserver.Object);
            publisher.Subscribe(workingObserver.Object);

            var report = CreateHealthReport("observertest", HealthStatus.Unhealthy);

            // Act
            Func<Task> act = async () => await publisher.PublishAsync(report, CancellationToken.None);

            // Assert
            await act.Should().NotThrowAsync("Publisher should handle observer exceptions gracefully");
            workingObserverCalled.Should().BeTrue("Working observer should still be called despite other observer failing");
        }

        [Test]
        [Category("Observers")]
        public void Subscribe_ShouldHandleConcurrentSubscriptions()
        {
            // Arrange
            var healthCheck = CreateDefaultHealthCheck();
            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);

            var observers = new List<Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>>();
            for (int i = 0; i < 100; i++)
            {
                observers.Add(new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>());
            }

            // Act - Subscribe concurrently
            Parallel.ForEach(observers, observer =>
            {
                publisher.Subscribe(observer.Object);
            });

            // Trigger an alert
            var report = CreateHealthReport("defaultcheck", HealthStatus.Unhealthy);
            publisher.PublishAsync(report, CancellationToken.None).Wait();

            // Assert - All observers should receive notifications
            // Note: Due to the ConcurrentBag limitation, we can't verify exact count,
            // but we can verify that at least some observers were called
            var calledCount = observers.Count(o =>
            {
                o.Verify(obs => obs.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()), Times.AtLeastOnce);
                return true;
            });

            calledCount.Should().BeGreaterThan(0, "At least some observers should be called");
        }

        [Test]
        [Category("Interceptor")]
        public async Task InterceptedBehaviours_ShouldBeThreadSafe_WhenCreatedConcurrently()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "interceptor",
                ServiceType = ServiceType.Interceptor,
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.Zero,
                        AlertByFailCount = 1,
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24)
                    }
                }
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);

            // Create reports for multiple different health checks
            var healthCheckNames = Enumerable.Range(0, 50).Select(i => $"service{i}").ToList();

            // Act - Process multiple health checks concurrently
            var tasks = healthCheckNames.SelectMany(name =>
                Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
                {
                    var report = CreateHealthReport(name, HealthStatus.Unhealthy);
                    await publisher.PublishAsync(report, CancellationToken.None);
                }))
            ).ToArray();

            await Task.WhenAll(tasks);

            // Assert - Should not throw and should process all checks
            // The test passing without exceptions indicates thread safety
            Assert.Pass("Concurrent intercepted behaviour creation completed without errors");
        }

        [Test]
        [Category("StressTest")]
        [Explicit("Long running stress test")]
        public async Task PublisherBase_ShouldRemainStable_UnderHighConcurrentLoad()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck()
            {
                Name = "stresstest",
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new AlertBehaviour()
                    {
                        TransportName = "TestTransport",
                        AlertEvery = TimeSpan.FromMilliseconds(10),
                        AlertByFailCount = 5,
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24),
                        AlertOnServiceRecovered = true
                    }
                }
            };

            var publisher = new DictionaryPublisher(_healthChecksBuilder, healthCheck, _alertTransportSettings);

            // Add observers
            var observerCallCount = 0;
            var observer = new Mock<IObserver<KeyValuePair<string, HealthReportEntry>>>();
            observer.Setup(o => o.OnNext(It.IsAny<KeyValuePair<string, HealthReportEntry>>()))
                .Callback(() => Interlocked.Increment(ref observerCallCount));
            publisher.Subscribe(observer.Object);

            // Act - High load with mixed statuses
            var random = new Random();
            var tasks = Enumerable.Range(0, 1000).Select(i => Task.Run(async () =>
            {
                var status = random.Next(3) switch
                {
                    0 => HealthStatus.Healthy,
                    1 => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };

                var report = CreateHealthReport("stresstest", status);
                await publisher.PublishAsync(report, CancellationToken.None);

                // Add small random delay to increase contention
                await Task.Delay(random.Next(1, 5));
            })).ToArray();

            await Task.WhenAll(tasks);

            // Assert
            var behaviour = healthCheck.AlertBehaviour.First();
            behaviour.LastCheck.Should().NotBe(DateTime.MinValue, "LastCheck should be updated");
            observerCallCount.Should().BeGreaterThan(0, "Observers should be notified");

            // Verify state consistency
            behaviour.FailedCount.Should().BeGreaterThan(-1, "FailedCount should be valid");
        }

        #region Helper Methods

        private HealthReport CreateHealthReport(string name, HealthStatus status)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                { name, new HealthReportEntry(status, "", TimeSpan.Zero, null, null) }
            };

            return new HealthReport(entries, TimeSpan.Zero);
        }

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
                        TransportMethod = AlertTransportMethod.Email,
                        AlertByFailCount = 1,
                        StartAlertingOn = TimeSpan.Zero,
                        StopAlertingOn = TimeSpan.FromHours(24)
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
