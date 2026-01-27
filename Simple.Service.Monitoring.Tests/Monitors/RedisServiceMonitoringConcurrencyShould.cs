using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    [Category("Concurrency")]
    public class RedisServiceMonitoringConcurrencyShould
    {
        private IHealthChecksBuilder healthChecksBuilder;
        
        [SetUp]
        public void Setup()
        {
            var healthcheckbuildermock = new Moq.Mock<IHealthChecksBuilder>();
            healthcheckbuildermock
                .Setup(m => m.Services)
                .Returns(new ServiceCollection());
            healthChecksBuilder = healthcheckbuildermock.Object;
        }

        [Test]
        public void Given_Valid_Redis_Monitoring_Settings()
        {
            // Arrange
            var redisCheck = new ServiceHealthCheck()
            {
                Name = "test-redis-healthcheck",
                AlertBehaviour = null,
                ConnectionString = "localhost:6379",
                ServiceType = ServiceType.Redis,
                HealthCheckConditions = new HealthCheckConditions()
                {
                    RedisBehaviour = new RedisBehaviour()
                    {
                        TimeOutMs = 5000
                    }
                }
            };
            
            // Act
            var redisMonitoring = new RedisServiceMonitoring(healthChecksBuilder, redisCheck);
            
            // Assert
            Action act = () => redisMonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public async Task Should_Handle_Multiple_Concurrent_HealthChecks_Without_ObjectDisposedException()
        {
            // Arrange
            var connectionString = "localhost:6379,abortConnect=false,connectTimeout=1000,syncTimeout=1000";
            var timeout = TimeSpan.FromMilliseconds(1000);
            
            var healthCheckInstances = Enumerable.Range(0, 10)
                .Select(_ => new RedisHealthCheck(connectionString, timeout))
                .ToList();

            var healthCheckContext = new HealthCheckContext();
            var cancellationToken = CancellationToken.None;
            
            // Act - Execute multiple health checks concurrently to simulate race condition
            var tasks = healthCheckInstances.Select(async healthCheck => 
            {
                try
                {
                    // Add some random delay to increase chance of race conditions
                    await Task.Delay(Random.Shared.Next(0, 50));
                    return await healthCheck.CheckHealthAsync(healthCheckContext, cancellationToken);
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy($"Exception during concurrent check: {ex.Message}", ex);
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            
            // Assert - None of the health checks should throw ObjectDisposedException
            results.Should().NotBeNull();
            results.Should().HaveCount(10);
            
            // Verify that we don't get ObjectDisposedException
            var objectDisposedErrors = results
                .Where(r => r.Status == HealthStatus.Unhealthy)
                .Where(r => r.Description != null && r.Description.Contains("Cannot access a disposed object"))
                .ToList();
            
            objectDisposedErrors.Should().BeEmpty(
                "health checks should not fail with ObjectDisposedException even when run concurrently");
        }

        [Test]
        public async Task Should_Create_And_Dispose_Separate_Connections_For_Each_HealthCheck()
        {
            // Arrange
            var connectionString = "localhost:6379,abortConnect=false,connectTimeout=500";
            var timeout = TimeSpan.FromMilliseconds(500);
            
            var healthCheck1 = new RedisHealthCheck(connectionString, timeout);
            var healthCheck2 = new RedisHealthCheck(connectionString, timeout);
            var healthCheck3 = new RedisHealthCheck(connectionString, timeout);
            
            var healthCheckContext = new HealthCheckContext();
            var cancellationToken = CancellationToken.None;
            
            // Act - Execute health checks sequentially
            var result1 = await healthCheck1.CheckHealthAsync(healthCheckContext, cancellationToken);
            var result2 = await healthCheck2.CheckHealthAsync(healthCheckContext, cancellationToken);
            var result3 = await healthCheck3.CheckHealthAsync(healthCheckContext, cancellationToken);
            
            // Assert - All should complete without affecting each other
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result3.Should().NotBeNull();
            
            // Note: We expect unhealthy if Redis is not running, but no ObjectDisposedException
            if (result1.Status == HealthStatus.Unhealthy)
            {
                result1.Description.Should().NotContain("Cannot access a disposed object");
            }
            if (result2.Status == HealthStatus.Unhealthy)
            {
                result2.Description.Should().NotContain("Cannot access a disposed object");
            }
            if (result3.Status == HealthStatus.Unhealthy)
            {
                result3.Description.Should().NotContain("Cannot access a disposed object");
            }
        }

        [Test]
        public async Task Should_Handle_Rapid_Sequential_HealthChecks_Without_Connection_Conflicts()
        {
            // Arrange
            var connectionString = "localhost:6379,abortConnect=false";
            var timeout = TimeSpan.FromMilliseconds(2000);
            var healthCheck = new RedisHealthCheck(connectionString, timeout);
            var healthCheckContext = new HealthCheckContext();
            var cancellationToken = CancellationToken.None;
            
            var results = new List<HealthCheckResult>();
            
            // Act - Execute same health check instance rapidly in sequence
            for (int i = 0; i < 20; i++)
            {
                var result = await healthCheck.CheckHealthAsync(healthCheckContext, cancellationToken);
                results.Add(result);
                
                // Small delay between checks
                await Task.Delay(10);
            }
            
            // Assert
            results.Should().HaveCount(20);
            
            // Check that no ObjectDisposedException occurred
            var disposedErrors = results
                .Where(r => r.Description != null && r.Description.Contains("Cannot access a disposed object"))
                .ToList();
                
            disposedErrors.Should().BeEmpty(
                "rapid sequential health checks should not cause ObjectDisposedException");
        }

        [Test]
        public async Task Should_Handle_Concurrent_And_Overlapping_HealthChecks_Stress_Test()
        {
            // Arrange - Simulate a realistic scenario with multiple health check instances
            // being called concurrently (like in a production environment with multiple threads)
            var connectionString = "localhost:6379,abortConnect=false,connectTimeout=2000";
            var timeout = TimeSpan.FromMilliseconds(2000);
            
            var numberOfConcurrentHealthChecks = 50;
            var numberOfIterations = 5;
            
            // Act
            var allTasks = new List<Task<HealthCheckResult>>();
            
            for (int iteration = 0; iteration < numberOfIterations; iteration++)
            {
                var healthCheckInstance = new RedisHealthCheck(connectionString, timeout);
                var healthCheckContext = new HealthCheckContext();
                
                for (int i = 0; i < numberOfConcurrentHealthChecks; i++)
                {
                    var task = Task.Run(async () =>
                    {
                        // Random delay to simulate real-world timing variations
                        await Task.Delay(Random.Shared.Next(0, 100));
                        return await healthCheckInstance.CheckHealthAsync(
                            healthCheckContext, 
                            CancellationToken.None);
                    });
                    
                    allTasks.Add(task);
                }
                
                // Small delay between iterations
                await Task.Delay(50);
            }
            
            var results = await Task.WhenAll(allTasks);
            
            // Assert
            results.Should().HaveCount(numberOfConcurrentHealthChecks * numberOfIterations);
            
            // Verify no ObjectDisposedException occurred
            var objectDisposedErrors = results
                .Where(r => r.Description != null && 
                           r.Description.Contains("Cannot access a disposed object"))
                .ToList();
            
            objectDisposedErrors.Should().BeEmpty(
                $"stress test with {numberOfConcurrentHealthChecks * numberOfIterations} concurrent health checks " +
                "should not cause ObjectDisposedException");
            
            // Log summary
            var healthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
            var unhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);
            var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);
            
            TestContext.WriteLine($"Results: Healthy={healthyCount}, Unhealthy={unhealthyCount}, Degraded={degradedCount}");
            TestContext.WriteLine($"Total checks: {results.Length}");
        }

        [Test]
        public async Task Should_Not_Share_Connections_Between_Different_HealthCheck_Instances()
        {
            // Arrange - Create multiple health check instances with different connection strings
            var connectionString1 = "server1:6379,abortConnect=false";
            var connectionString2 = "server2:6379,abortConnect=false";
            var connectionString3 = "server3:6379,abortConnect=false";
            
            var timeout = TimeSpan.FromMilliseconds(1000);
            
            var healthCheck1 = new RedisHealthCheck(connectionString1, timeout);
            var healthCheck2 = new RedisHealthCheck(connectionString2, timeout);
            var healthCheck3 = new RedisHealthCheck(connectionString3, timeout);
            
            var healthCheckContext = new HealthCheckContext();
            
            // Act - Execute all concurrently
            var tasks = new[]
            {
                healthCheck1.CheckHealthAsync(healthCheckContext, CancellationToken.None),
                healthCheck2.CheckHealthAsync(healthCheckContext, CancellationToken.None),
                healthCheck3.CheckHealthAsync(healthCheckContext, CancellationToken.None)
            };
            
            var results = await Task.WhenAll(tasks);
            
            // Assert - Each should fail independently without affecting others
            results.Should().HaveCount(3);
            
            // Since these are different servers, they should all fail to connect
            // but importantly, none should have ObjectDisposedException
            foreach (var result in results)
            {
                if (result.Status == HealthStatus.Unhealthy && result.Description != null)
                {
                    result.Description.Should().NotContain("Cannot access a disposed object",
                        "each health check should use its own connection");
                }
            }
        }
    }
}
