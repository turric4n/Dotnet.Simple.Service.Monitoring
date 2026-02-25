using FluentAssertions;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Testcontainers.Redis;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    [NonParallelizable]
    public class DockerRedisAcceptanceTests : DockerTestBase
    {
        private RedisContainer _redisContainer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Create and start Redis container
            _redisContainer = new RedisBuilder()
                .WithCleanUp(true)
                .Build();

            await _redisContainer.StartAsync();

            // Seed some test data
            await SeedRedisDataAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();
            
            if (_redisContainer != null)
            {
                await _redisContainer.StopAsync();
                await _redisContainer.DisposeAsync();
            }
        }

        private async Task SeedRedisDataAsync()
        {
            var connectionString = _redisContainer.GetConnectionString();
            var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var db = redis.GetDatabase();

            // Set some test values
            await db.StringSetAsync("test:key1", "value1");
            await db.StringSetAsync("test:key2", "value2");
            await db.HashSetAsync("user:1", new HashEntry[]
            {
                new HashEntry("name", "John Doe"),
                new HashEntry("email", "john@example.com")
            });

            redis.Dispose();
        }

        [Test]
        public async Task Should_Monitor_Redis_Connection_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Redis Test",
                ServiceType = ServiceType.Redis,
                ConnectionString = _redisContainer.GetConnectionString(),
                HealthCheckConditions = new HealthCheckConditions
                {
                    RedisBehaviour = new RedisBehaviour
                    {
                        TimeOutMs = 5000
                    }
                }
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Monitor_Redis_With_Short_Timeout()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Redis Timeout Test",
                ServiceType = ServiceType.Redis,
                ConnectionString = _redisContainer.GetConnectionString(),
                HealthCheckConditions = new HealthCheckConditions
                {
                    RedisBehaviour = new RedisBehaviour
                    {
                        TimeOutMs = 1000
                    }
                }
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act
            var response = await GetHealthCheckResponseAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Test]
        public async Task Should_Verify_Redis_Can_Read_And_Write()
        {
            // Arrange - First verify we can interact with Redis
            var connectionString = _redisContainer.GetConnectionString();
            var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var db = redis.GetDatabase();
            
            // Write and read a value
            await db.StringSetAsync("health:check", "test");
            var value = await db.StringGetAsync("health:check");
            value.ToString().Should().Be("test");
            
            await redis.DisposeAsync();

            // Now test the health check
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Redis Read/Write Test",
                ServiceType = ServiceType.Redis,
                ConnectionString = connectionString,
                HealthCheckConditions = new HealthCheckConditions
                {
                    RedisBehaviour = new RedisBehaviour
                    {
                        TimeOutMs = 3000
                    }
                }
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Handle_Multiple_Concurrent_HealthChecks_Against_Real_Redis()
        {
            // Arrange - This test verifies the race condition fix works with real Redis
            var connectionString = _redisContainer.GetConnectionString();
            var timeout = System.TimeSpan.FromMilliseconds(5000);
            
            // Create multiple health check instances
            var healthCheckInstances = Enumerable.Range(0, 20)
                .Select(_ => new RedisHealthCheck(connectionString, timeout))
                .ToList();

            var healthCheckContext = new HealthCheckContext();
            
            // Act - Execute all health checks concurrently
            var tasks = healthCheckInstances.Select(async healthCheck => 
            {
                // Add random delay to increase chance of race conditions
                await Task.Delay(System.Random.Shared.Next(0, 100));
                return await healthCheck.CheckHealthAsync(healthCheckContext, CancellationToken.None);
            }).ToList();

            var results = await Task.WhenAll(tasks);
            
            // Assert - All checks should succeed and none should have ObjectDisposedException
            results.Should().HaveCount(20);
            results.Should().OnlyContain(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                "all health checks should succeed against real Redis");
            
            // Verify no ObjectDisposedException in any result
            var disposedErrors = results
                .Where(r => r.Description != null && r.Description.Contains("Cannot access a disposed object"))
                .ToList();
                
            disposedErrors.Should().BeEmpty(
                "no health check should fail with ObjectDisposedException even when run concurrently against real Redis");
        }

        [Test]
        public async Task Should_Handle_Rapid_Sequential_Checks_Against_Real_Redis()
        {
            // Arrange
            var connectionString = _redisContainer.GetConnectionString();
            var timeout = System.TimeSpan.FromMilliseconds(3000);
            var healthCheck = new RedisHealthCheck(connectionString, timeout);
            var healthCheckContext = new HealthCheckContext();
            
            var results = new List<HealthCheckResult>();
            
            // Act - Execute same health check instance rapidly 30 times
            for (int i = 0; i < 30; i++)
            {
                var result = await healthCheck.CheckHealthAsync(healthCheckContext, CancellationToken.None);
                results.Add(result);
                
                // Very short delay
                await Task.Delay(10);
            }
            
            // Assert
            results.Should().HaveCount(30);
            results.Should().OnlyContain(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy,
                "all rapid sequential checks should succeed");
            
            var disposedErrors = results
                .Where(r => r.Description != null && r.Description.Contains("Cannot access a disposed object"))
                .ToList();
                
            disposedErrors.Should().BeEmpty(
                "rapid sequential health checks should not cause ObjectDisposedException");
        }

        [Test]
        public async Task Should_Monitor_Multiple_Redis_Instances_Concurrently()
        {
            // Arrange - Multiple health checks configured for the same Redis instance
            var healthChecks = new List<ServiceHealthCheck>
            {
                new ServiceHealthCheck
                {
                    Name = "Redis Instance 1",
                    ServiceType = ServiceType.Redis,
                    ConnectionString = _redisContainer.GetConnectionString(),
                    HealthCheckConditions = new HealthCheckConditions
                    {
                        RedisBehaviour = new RedisBehaviour { TimeOutMs = 5000 }
                    }
                },
                new ServiceHealthCheck
                {
                    Name = "Redis Instance 2",
                    ServiceType = ServiceType.Redis,
                    ConnectionString = _redisContainer.GetConnectionString(),
                    HealthCheckConditions = new HealthCheckConditions
                    {
                        RedisBehaviour = new RedisBehaviour { TimeOutMs = 5000 }
                    }
                },
                new ServiceHealthCheck
                {
                    Name = "Redis Instance 3",
                    ServiceType = ServiceType.Redis,
                    ConnectionString = _redisContainer.GetConnectionString(),
                    HealthCheckConditions = new HealthCheckConditions
                    {
                        RedisBehaviour = new RedisBehaviour { TimeOutMs = 5000 }
                    }
                }
            };

            Server = await CreateTestServerAsync(healthChecks);

            // Act - Multiple calls to health check endpoint
            var responseTasks = Enumerable.Range(0, 10)
                .Select(_ => GetHealthCheckResponseAsync())
                .ToList();

            var responses = await Task.WhenAll(responseTasks);

            // Assert - All responses should be successful
            responses.Should().OnlyContain(r => r.IsSuccessStatusCode,
                "all concurrent health check requests should succeed");

            foreach (var response in responses)
            {
                var content = await response.Content.ReadAsStringAsync();
                content.Should().Contain("Healthy");
                content.Should().NotContain("ObjectDisposedException");
                content.Should().NotContain("Cannot access a disposed object");
            }
        }

        [Test]
        public async Task Should_Handle_Connection_Stress_Test_With_Real_Redis()
        {
            // Arrange - Stress test with many concurrent checks
            var connectionString = _redisContainer.GetConnectionString();
            var timeout = System.TimeSpan.FromMilliseconds(10000); // Increased timeout for stress test
            var numberOfChecks = 100;
            
            // Act - Create and execute many health checks concurrently
            var tasks = Enumerable.Range(0, numberOfChecks).Select(async i =>
            {
                var healthCheck = new RedisHealthCheck(connectionString, timeout);
                var context = new HealthCheckContext();
                
                // Stagger the checks more to avoid overwhelming the container
                await Task.Delay(System.Random.Shared.Next(0, 200));
                
                return await healthCheck.CheckHealthAsync(context, CancellationToken.None);
            }).ToList();

            var results = await Task.WhenAll(tasks);
            
            // Assert
            results.Should().HaveCount(numberOfChecks);
            
            var healthyCount = results.Count(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
            var unhealthyCount = results.Count(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy);
            var degradedCount = results.Count(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded);
            
            // Most importantly, verify no ObjectDisposedException
            var disposedErrors = results
                .Where(r => r.Description != null && r.Description.Contains("Cannot access a disposed object"))
                .ToList();
                
            disposedErrors.Should().BeEmpty(
                $"stress test with {numberOfChecks} concurrent checks should not cause ObjectDisposedException");
            
            // We expect most to be healthy, but some may timeout under extreme load
            // The key is that there are NO ObjectDisposedExceptions
            healthyCount.Should().BeGreaterThan((int)(numberOfChecks * 0.9), 
                $"at least 90% of {numberOfChecks} checks should be healthy against real Redis");
            
            TestContext.WriteLine($"Stress test completed: {healthyCount} healthy, {unhealthyCount} unhealthy, {degradedCount} degraded out of {numberOfChecks} checks");
            TestContext.WriteLine($"Success rate: {(healthyCount * 100.0 / numberOfChecks):F2}%");
            
            // Log any failures for investigation (excluding timeout/connection errors which are expected under stress)
            var actualErrors = results
                .Where(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy)
                .Where(r => r.Description != null && 
                           !r.Description.Contains("timeout") && 
                           !r.Description.Contains("Timeout") &&
                           !r.Description.Contains("Unable to connect"))
                .ToList();
                
            foreach (var error in actualErrors)
            {
                TestContext.WriteLine($"Unexpected error: {error.Description}");
            }
        }

        [Test]
        public async Task Should_Handle_Controlled_Concurrent_Checks_With_Perfect_Success_Rate()
        {
            // Arrange - More controlled test with guaranteed success
            // This test proves the fix works without stress-induced timeouts
            var connectionString = _redisContainer.GetConnectionString();
            var timeout = System.TimeSpan.FromMilliseconds(5000);
            var numberOfChecks = 50; // Reduced for reliability
            
            // Use batching to avoid overwhelming the container
            var batchSize = 10;
            var results = new List<HealthCheckResult>();
            
            // Act - Execute in batches
            for (int batch = 0; batch < numberOfChecks / batchSize; batch++)
            {
                var batchTasks = Enumerable.Range(0, batchSize).Select(async i =>
                {
                    var healthCheck = new RedisHealthCheck(connectionString, timeout);
                    var context = new HealthCheckContext();
                    
                    // Small random delay within batch
                    await Task.Delay(System.Random.Shared.Next(0, 50));
                    
                    return await healthCheck.CheckHealthAsync(context, CancellationToken.None);
                }).ToList();

                var batchResults = await Task.WhenAll(batchTasks);
                results.AddRange(batchResults);
                
                // Small delay between batches
                await Task.Delay(100);
            }
            
            // Assert
            results.Should().HaveCount(numberOfChecks);
            
            var healthyCount = results.Count(r => r.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy);
            
            // Verify no ObjectDisposedException - this is the critical assertion
            var disposedErrors = results
                .Where(r => r.Description != null && r.Description.Contains("Cannot access a disposed object"))
                .ToList();
                
            disposedErrors.Should().BeEmpty(
                "no ObjectDisposedException should occur in controlled concurrent test");
            
            // With batching and controlled timing, we expect 100% success
            healthyCount.Should().Be(numberOfChecks, 
                $"all {numberOfChecks} checks should succeed with controlled concurrency");
            
            TestContext.WriteLine($"Controlled concurrency test: {healthyCount}/{numberOfChecks} successful (100%)");
        }

        [TearDown]
        public async Task TearDown()
        {
            await CleanupTestServerAsync();
        }
    }
}
