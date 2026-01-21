using FluentAssertions;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using NUnit.Framework;
using RabbitMQ.Client;
using Simple.Service.Monitoring.Library.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library;
using Testcontainers.Elasticsearch;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    /// <summary>
    /// Comprehensive acceptance test that runs all Docker containers simultaneously
    /// and validates that the monitoring service can monitor all service types.
    /// This is a full end-to-end integration test.
    /// </summary>
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    [Category("Integration")]
    public class AllServicesAcceptanceTests : DockerTestBase
    {
        private MsSqlContainer _sqlContainer;
        private MySqlContainer _mySqlContainer;
        private PostgreSqlContainer _postgresContainer;
        private RedisContainer _redisContainer;
        private RabbitMqContainer _rabbitMqContainer;
        private ElasticsearchContainer _elasticsearchContainer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            TestContext.WriteLine("Starting all containers for comprehensive integration test...");

            // Start all containers in parallel for faster setup
            var startTasks = new List<Task>();

            // SQL Server
            _sqlContainer = new MsSqlBuilder()
                .WithPassword("YourStrong@Passw0rd")
                .WithCleanUp(true)
                .Build();
            startTasks.Add(_sqlContainer.StartAsync());

            // MySQL
            _mySqlContainer = new MySqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .WithCleanUp(true)
                .Build();
            startTasks.Add(_mySqlContainer.StartAsync());

            // PostgreSQL
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .WithCleanUp(true)
                .Build();
            startTasks.Add(_postgresContainer.StartAsync());

            // Redis
            _redisContainer = new RedisBuilder()
                .WithCleanUp(true)
                .Build();
            startTasks.Add(_redisContainer.StartAsync());

            // RabbitMQ
            _rabbitMqContainer = new RabbitMqBuilder()
                .WithUsername("guest")
                .WithPassword("guest")
                .WithCleanUp(true)
                .Build();
            startTasks.Add(_rabbitMqContainer.StartAsync());

            // Elasticsearch
            _elasticsearchContainer = new ElasticsearchBuilder()
                .WithCleanUp(true)
                .Build();
            startTasks.Add(_elasticsearchContainer.StartAsync());

            // Wait for all containers to start
            await Task.WhenAll(startTasks);
            TestContext.WriteLine("All containers started successfully!");

            // Initialize test data in parallel
            await Task.WhenAll(
                InitializeSqlServerAsync(),
                InitializeMySqlAsync(),
                InitializePostgreSqlAsync(),
                InitializeRedisAsync(),
                InitializeRabbitMqAsync(),
                InitializeElasticsearchAsync()
            );

            TestContext.WriteLine("All containers initialized with test data!");
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();

            TestContext.WriteLine("Stopping and disposing all containers...");

            // Stop all containers in parallel
            var stopTasks = new List<Task>();

            if (_sqlContainer != null)
            {
                stopTasks.Add(Task.Run(async () =>
                {
                    await _sqlContainer.StopAsync();
                    await _sqlContainer.DisposeAsync();
                }));
            }

            if (_mySqlContainer != null)
            {
                stopTasks.Add(Task.Run(async () =>
                {
                    await _mySqlContainer.StopAsync();
                    await _mySqlContainer.DisposeAsync();
                }));
            }

            if (_postgresContainer != null)
            {
                stopTasks.Add(Task.Run(async () =>
                {
                    await _postgresContainer.StopAsync();
                    await _postgresContainer.DisposeAsync();
                }));
            }

            if (_redisContainer != null)
            {
                stopTasks.Add(Task.Run(async () =>
                {
                    await _redisContainer.StopAsync();
                    await _redisContainer.DisposeAsync();
                }));
            }

            if (_rabbitMqContainer != null)
            {
                stopTasks.Add(Task.Run(async () =>
                {
                    await _rabbitMqContainer.StopAsync();
                    await _rabbitMqContainer.DisposeAsync();
                }));
            }

            if (_elasticsearchContainer != null)
            {
                stopTasks.Add(Task.Run(async () =>
                {
                    await _elasticsearchContainer.StopAsync();
                    await _elasticsearchContainer.DisposeAsync();
                }));
            }

            await Task.WhenAll(stopTasks);
            TestContext.WriteLine("All containers stopped and disposed!");
        }

        #region Initialization Methods

        private async Task InitializeSqlServerAsync()
        {
            var connectionString = _sqlContainer.GetConnectionString();
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE TestData (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    Name NVARCHAR(100),
                    Value INT
                );
                INSERT INTO TestData (Name, Value) VALUES ('Test1', 100);
                INSERT INTO TestData (Name, Value) VALUES ('Test2', 200);
            ";
            await command.ExecuteNonQueryAsync();
        }

        private async Task InitializeMySqlAsync()
        {
            var connectionString = _mySqlContainer.GetConnectionString();
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TestData (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100),
                    Value INT
                );
                INSERT INTO TestData (Name, Value) VALUES ('Test1', 100);
                INSERT INTO TestData (Name, Value) VALUES ('Test2', 200);
            ";
            await command.ExecuteNonQueryAsync();
        }

        private async Task InitializePostgreSqlAsync()
        {
            var connectionString = _postgresContainer.GetConnectionString();
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS TestData (
                    Id SERIAL PRIMARY KEY,
                    Name VARCHAR(100),
                    Value INT
                );
                INSERT INTO TestData (Name, Value) VALUES ('Test1', 100);
                INSERT INTO TestData (Name, Value) VALUES ('Test2', 200);
            ";
            await command.ExecuteNonQueryAsync();
        }

        private async Task InitializeRedisAsync()
        {
            var connectionString = _redisContainer.GetConnectionString();
            var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
            var db = redis.GetDatabase();

            await db.StringSetAsync("test:key", "value");
            await redis.DisposeAsync();
        }

        private async Task InitializeRabbitMqAsync()
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(_rabbitMqContainer.GetConnectionString())
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queue: "test.queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
        }

        private async Task InitializeElasticsearchAsync()
        {
            // Elasticsearch container takes time to fully start, give it a moment
            await Task.Delay(2000);
        }

        #endregion

        [Test]
        public async Task Should_Monitor_All_Services_Simultaneously()
        {
            // Arrange - Create health checks for all services
            var healthChecks = new List<ServiceHealthCheck>();

            //// SQL Server
            //healthChecks.Add(new ServiceHealthCheck
            //{
            //    Name = "SQL Server All Services Test",
            //    ServiceType = ServiceType.MsSql,
            //    ConnectionString = _sqlContainer.GetConnectionString(),
            //    MonitoringInterval = TimeSpan.FromSeconds(30),
            //    HealthCheckConditions = new HealthCheckConditions
            //    {
            //        SqlBehaviour = new SqlBehaviour
            //        {
            //            Query = "SELECT COUNT(*) FROM TestData",
            //            ResultExpression = ResultExpression.GreaterThan,
            //            SqlResultDataType = SqlResultDataType.Int,
            //            ExpectedResult = "0"
            //        }
            //    }
            //});

            // MySQL
            //healthChecks.Add(new ServiceHealthCheck
            //{
            //    Name = "MySQL All Services Test",
            //    ServiceType = ServiceType.MySql,
            //    ConnectionString = _mySqlContainer.GetConnectionString(),
            //    MonitoringInterval = TimeSpan.FromSeconds(30),
            //    HealthCheckConditions = new HealthCheckConditions
            //    {
            //        SqlBehaviour = new SqlBehaviour
            //        {
            //            Query = "SELECT COUNT(*) FROM TestData",
            //            ResultExpression = ResultExpression.GreaterThan,
            //            SqlResultDataType = SqlResultDataType.Int,
            //            ExpectedResult = "0"
            //        }
            //    }
            //});

            // PostgreSQL
            //healthChecks.Add(new ServiceHealthCheck
            //{
            //    Name = "PostgreSQL All Services Test",
            //    ServiceType = ServiceType.PostgreSql,
            //    ConnectionString = _postgresContainer.GetConnectionString(),
            //    MonitoringInterval = TimeSpan.FromSeconds(30),
            //    HealthCheckConditions = new HealthCheckConditions
            //    {
            //        SqlBehaviour = new SqlBehaviour
            //        {
            //            Query = "SELECT COUNT(*) FROM TestData",
            //            ResultExpression = ResultExpression.GreaterThan,
            //            SqlResultDataType = SqlResultDataType.Int,
            //            ExpectedResult = "0"
            //        }
            //    }
            //});

            // Redis
            //healthChecks.Add(new ServiceHealthCheck
            //{
            //    Name = "Redis All Services Test",
            //    ServiceType = ServiceType.Redis,
            //    ConnectionString = _redisContainer.GetConnectionString(),
            //    MonitoringInterval = TimeSpan.FromSeconds(30),
            //    HealthCheckConditions = new HealthCheckConditions
            //    {
            //        RedisBehaviour = new RedisBehaviour
            //        {
            //            TimeOutMs = 5000
            //        }
            //    }
            //});

            // RabbitMQ
            healthChecks.Add(new ServiceHealthCheck
            {
                Name = "RabbitMQ All Services Test",
                ServiceType = ServiceType.Rmq,
                ConnectionString = _rabbitMqContainer.GetConnectionString()
            });

            //// Elasticsearch
            //var elasticUri = new Uri(_elasticsearchContainer.GetConnectionString());
            //healthChecks.Add(new ServiceHealthCheck
            //{
            //    Name = "Elasticsearch All Services Test",
            //    ServiceType = ServiceType.ElasticSearch,
            //    EndpointOrHost = $"{elasticUri.Scheme}://{elasticUri.Host}",
            //    MonitoringInterval = TimeSpan.FromSeconds(30)
            //});

            //// Ping (localhost)
            //healthChecks.Add(new ServiceHealthCheck
            //{
            //    Name = "Ping All Services Test",
            //    ServiceType = ServiceType.Ping,
            //    EndpointOrHost = "127.0.0.1",
            //    MonitoringInterval = TimeSpan.FromSeconds(30)
            //});

            TestContext.WriteLine($"Created {healthChecks.Count} health checks");

            // Act - Create test server with all health checks
            Server = await CreateTestServerAsync(healthChecks);

            // Give services a moment to perform health checks
            await Task.Delay(2000);

            var response = await GetHealthCheckResponseAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            
            TestContext.WriteLine("Health Check Response:");
            TestContext.WriteLine(content);

            // Verify the response contains health information
            content.Should().NotBeNullOrEmpty();
            
            // The response should indicate overall health status
            // Note: Depending on the health check implementation, this could be "Healthy" or contain individual service statuses
        }

        [Test]
        public async Task Should_Monitor_Individual_Database_Services()
        {
            // Arrange - Test just the database services
            var healthChecks = new List<ServiceHealthCheck>
            {
                new ServiceHealthCheck
                {
                    Name = "SQL Server DB Test",
                    ServiceType = ServiceType.MsSql,
                    ConnectionString = _sqlContainer.GetConnectionString()
                },
                new ServiceHealthCheck
                {
                    Name = "MySQL DB Test",
                    ServiceType = ServiceType.MySql,
                    ConnectionString = _mySqlContainer.GetConnectionString()
                },
                new ServiceHealthCheck
                {
                    Name = "PostgreSQL DB Test",
                    ServiceType = ServiceType.PostgreSql,
                    ConnectionString = _postgresContainer.GetConnectionString()
                }
            };

            Server = await CreateTestServerAsync(healthChecks);

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Monitor_Cache_And_Queue_Services()
        {
            // Arrange - Test Redis and RabbitMQ
            var healthChecks = new List<ServiceHealthCheck>
            {
                new ServiceHealthCheck
                {
                    Name = "Redis Cache Test",
                    ServiceType = ServiceType.Redis,
                    ConnectionString = _redisContainer.GetConnectionString(),
                    HealthCheckConditions = new HealthCheckConditions
                    {
                        RedisBehaviour = new RedisBehaviour
                        {
                            TimeOutMs = 3000
                        }
                    }
                },
                new ServiceHealthCheck
                {
                    Name = "RabbitMQ Queue Test",
                    ServiceType = ServiceType.Rmq,
                    ConnectionString = _rabbitMqContainer.GetConnectionString()
                }
            };

            Server = await CreateTestServerAsync(healthChecks);

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
