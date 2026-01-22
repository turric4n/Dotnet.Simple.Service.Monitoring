using FluentAssertions;
using Npgsql;
using NUnit.Framework;
using Simple.Service.Monitoring.Library;
using Simple.Service.Monitoring.Library.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    [NonParallelizable]
    public class DockerPostgreSqlAcceptanceTests : DockerTestBase
    {
        private PostgreSqlContainer _postgresContainer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Create and start PostgreSQL container
            _postgresContainer = new PostgreSqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .WithCleanUp(true)
                .Build();

            await _postgresContainer.StartAsync();

            // Initialize test database and table
            await InitializeDatabaseAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();
            
            if (_postgresContainer != null)
            {
                await _postgresContainer.StopAsync();
                await _postgresContainer.DisposeAsync();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            var connectionString = _postgresContainer.GetConnectionString();
            
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Orders (
                    Id SERIAL PRIMARY KEY,
                    OrderNumber VARCHAR(50),
                    TotalAmount DECIMAL(10,2),
                    Status VARCHAR(20),
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                INSERT INTO Orders (OrderNumber, TotalAmount, Status) VALUES ('ORD-001', 150.00, 'Completed');
                INSERT INTO Orders (OrderNumber, TotalAmount, Status) VALUES ('ORD-002', 250.00, 'Pending');
                INSERT INTO Orders (OrderNumber, TotalAmount, Status) VALUES ('ORD-003', 350.00, 'Completed');
            ";
            
            await command.ExecuteNonQueryAsync();
        }

        [Test]
        public async Task Should_Monitor_PostgreSql_Connection_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "PostgreSQL Test",
                ServiceType = ServiceType.PostgreSql,
                ConnectionString = _postgresContainer.GetConnectionString()
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Monitor_PostgreSql_With_Custom_Query_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "PostgreSQL Query Test",
                ServiceType = ServiceType.PostgreSql,
                ConnectionString = _postgresContainer.GetConnectionString(),
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT COUNT(*) FROM Orders WHERE Status = 'Completed'",
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.Int,
                        ExpectedResult = "1"
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
        public async Task Should_Validate_Aggregate_Functions()
        {
            // Arrange - Use CAST to INT to avoid decimal formatting issues
            var healthCheck = new ServiceHealthCheck
            {
                Name = "PostgreSQL Aggregate Test",
                ServiceType = ServiceType.PostgreSql,
                ConnectionString = _postgresContainer.GetConnectionString(),
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT CAST(SUM(TotalAmount) AS INTEGER) FROM Orders",
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.Int,
                        ExpectedResult = "500"
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

        [TearDown]
        public async Task TearDown()
        {
            await CleanupTestServerAsync();
        }
    }
}
