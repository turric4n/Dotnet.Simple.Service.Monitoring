using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Simple.Service.Monitoring.Library;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using System;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    public class PostgreSqlServiceMonitoringShould
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
        public void Given_Valid_PostgreSql_Host_Monitoring_Settings()
        {
            // Arrange
            var postgresCheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=secret",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.PostgreSql,
                HealthCheckConditions = new HealthCheckConditions()
            };

            // Act
            var postgresMonitoring = new PostgreSqlServiceMonitoring(healthChecksBuilder, postgresCheck);

            // Assert
            Action act = () => postgresMonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Given_Valid_PostgreSql_Host_And_Query_Monitoring_Settings()
        {
            // Arrange
            var postgresCheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                ConnectionString = "Host=localhost;Database=testdb;Username=postgres;Password=secret",
                ServiceType = ServiceType.PostgreSql,
                MonitoringInterval = TimeSpan.FromSeconds(1),
                HealthCheckConditions = new HealthCheckConditions()
                {
                    SqlBehaviour = new SqlBehaviour()
                    {
                        Query = "SELECT COUNT(*) FROM users",
                        ExpectedResult = 10,
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.Int
                    }
                }
            };

            // Act
            var postgresMonitoring = new PostgreSqlServiceMonitoring(healthChecksBuilder, postgresCheck);

            // Assert
            Action act = () => postgresMonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Given_Null_ConnectionString_Should_Throw_Exception()
        {
            // Arrange
            var postgresCheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                ConnectionString = null,
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.PostgreSql,
                HealthCheckConditions = new HealthCheckConditions()
            };

            // Act
            var postgresMonitoring = new PostgreSqlServiceMonitoring(healthChecksBuilder, postgresCheck);

            // Assert
            Action act = () => postgresMonitoring.SetUp();
            act.Should().Throw<Exception>();
        }
    }
}
