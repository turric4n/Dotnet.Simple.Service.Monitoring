using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Simple.Service.Monitoring.Library;
using Simple.Service.Monitoring.Library.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Testcontainers.MsSql;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    public class DockerSqlServerAcceptanceTests : DockerTestBase
    {
        private MsSqlContainer _sqlContainer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Create and start SQL Server container
            _sqlContainer = new MsSqlBuilder()
                .WithPassword("YourStrong@Passw0rd")
                .WithCleanUp(true)
                .Build();

            await _sqlContainer.StartAsync();

            // Initialize test database and table
            await InitializeDatabaseAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();
            
            if (_sqlContainer != null)
            {
                await _sqlContainer.StopAsync();
                await _sqlContainer.DisposeAsync();
            }
        }

        private async Task InitializeDatabaseAsync()
        {
            var connectionString = _sqlContainer.GetConnectionString();
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE TestUsers (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    Name NVARCHAR(100),
                    CreatedAt DATETIME2 DEFAULT GETDATE()
                );
                
                INSERT INTO TestUsers (Name) VALUES ('Test User 1');
                INSERT INTO TestUsers (Name) VALUES ('Test User 2');
                INSERT INTO TestUsers (Name) VALUES ('Test User 3');
            ";
            
            await command.ExecuteNonQueryAsync();
        }

        [Test]
        public async Task Should_Monitor_SqlServer_Connection_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "SQL Server Test",
                ServiceType = ServiceType.MsSql,
                ConnectionString = _sqlContainer.GetConnectionString(),
                MonitoringInterval = System.TimeSpan.FromSeconds(30)
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Monitor_SqlServer_With_Custom_Query_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "SQL Server Query Test",
                ServiceType = ServiceType.MsSql,
                ConnectionString = _sqlContainer.GetConnectionString(),
                MonitoringInterval = System.TimeSpan.FromSeconds(30),
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT COUNT(*) FROM TestUsers",
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.Int,
                        ExpectedResult = "0"
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
        public async Task Should_Detect_Failed_Query_Validation()
        {
            // Arrange - Create a health check that expects no users (should fail)
            var healthCheck = new ServiceHealthCheck
            {
                Name = "SQL Server Failed Query Test",
                ServiceType = ServiceType.MsSql,
                ConnectionString = _sqlContainer.GetConnectionString(),
                MonitoringInterval = System.TimeSpan.FromSeconds(30),
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT COUNT(*) FROM TestUsers",
                        ResultExpression = ResultExpression.Equal,
                        SqlResultDataType = SqlResultDataType.Int,
                        ExpectedResult = "0" // We have 3 users, so this should fail
                    }
                }
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act
            var response = await GetHealthCheckResponseAsync();

            // Assert - Health check should return unhealthy status
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Unhealthy");
        }

        [TearDown]
        public async Task TearDown()
        {
            await CleanupTestServerAsync();
        }
    }
}
