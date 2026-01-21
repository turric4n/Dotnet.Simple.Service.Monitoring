using FluentAssertions;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Testcontainers.Redis;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
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
            
            redis.Dispose();

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

        [TearDown]
        public async Task TearDown()
        {
            await CleanupTestServerAsync();
        }
    }
}
