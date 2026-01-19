using FluentAssertions;
using MySqlConnector;
using NUnit.Framework;
using Simple.Service.Monitoring.Library;
using Simple.Service.Monitoring.Library.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Testcontainers.MySql;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    public class DockerMySqlAcceptanceTests : DockerTestBase
    {
        private MySqlContainer _mySqlContainer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Create and start MySQL container
            _mySqlContainer = new MySqlBuilder()
                .WithDatabase("testdb")
                .WithUsername("testuser")
                .WithPassword("testpassword")
                .WithCleanUp(true)
                .Build();

            await _mySqlContainer.StartAsync();

            // Initialize test database and table
            await InitializeDatabaseAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();
            
            if (_mySqlContainer != null)
            {
                await _mySqlContainer.StopAsync();
                await _mySqlContainer.DisposeAsync();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            var connectionString = _mySqlContainer.GetConnectionString();
            
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100),
                    Price DECIMAL(10,2),
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
                
                INSERT INTO Products (Name, Price) VALUES ('Product A', 19.99);
                INSERT INTO Products (Name, Price) VALUES ('Product B', 29.99);
                INSERT INTO Products (Name, Price) VALUES ('Product C', 39.99);
            ";
            
            await command.ExecuteNonQueryAsync();
        }

        [Test]
        public async Task Should_Monitor_MySql_Connection_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "MySQL Test",
                ServiceType = ServiceType.MySql,
                ConnectionString = _mySqlContainer.GetConnectionString(),
                MonitoringInterval = System.TimeSpan.FromSeconds(30)
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Monitor_MySql_With_Custom_Query_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "MySQL Query Test",
                ServiceType = ServiceType.MySql,
                ConnectionString = _mySqlContainer.GetConnectionString(),
                MonitoringInterval = System.TimeSpan.FromSeconds(30),
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT COUNT(*) FROM Products WHERE Price > 20",
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.Int,
                        ExpectedResult = "0"
                    }
                }
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act
            var response = await GetHealthCheckResponseAsync();
            var content = await response.Content.ReadAsStringAsync();
            
            // Debug output
            TestContext.WriteLine($"Status Code: {response.StatusCode}");
            TestContext.WriteLine($"Response Content: {content}");
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue($"Expected success but got {response.StatusCode}. Content: {content}");
            content.Should().Contain("Healthy");
        }

        [Test]
        public async Task Should_Validate_Query_Results_With_Decimal_Values()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "MySQL Decimal Query Test",
                ServiceType = ServiceType.MySql,
                ConnectionString = _mySqlContainer.GetConnectionString(),
                MonitoringInterval = System.TimeSpan.FromSeconds(30),
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT AVG(Price) FROM Products",
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.String,
                        ExpectedResult = "25"
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
