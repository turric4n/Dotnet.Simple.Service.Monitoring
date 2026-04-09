using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Simple.Service.Monitoring.Library;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture]
    [Category("Unit")]
    public class SqliteServiceMonitoringShould
    {
        private IHealthChecksBuilder _healthChecksBuilder;

        [SetUp]
        public void Setup()
        {
            var mock = new Moq.Mock<IHealthChecksBuilder>();
            mock.Setup(m => m.Services).Returns(new ServiceCollection());
            _healthChecksBuilder = mock.Object;
        }

        [Test]
        public void Given_Valid_Sqlite_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqlite_test",
                ServiceType = ServiceType.Sqlite,
                ConnectionString = "Data Source=mydb.sqlite",
                HealthCheckConditions = new HealthCheckConditions()
            };

            var monitoring = new SqliteServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_ConnectionString_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqlite_test",
                ServiceType = ServiceType.Sqlite,
                ConnectionString = null,
                HealthCheckConditions = new HealthCheckConditions()
            };

            var monitoring = new SqliteServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Throw_When_ConnectionString_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqlite_test",
                ServiceType = ServiceType.Sqlite,
                ConnectionString = "",
                HealthCheckConditions = new HealthCheckConditions()
            };

            var monitoring = new SqliteServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Given_Valid_Sqlite_With_Custom_Query()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqlite_query",
                ServiceType = ServiceType.Sqlite,
                ConnectionString = "Data Source=app.db;Version=3;",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT COUNT(*) FROM users",
                        ExpectedResult = 0,
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.Int
                    }
                }
            };

            var monitoring = new SqliteServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Given_Valid_InMemory_Sqlite()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqlite_memory",
                ServiceType = ServiceType.Sqlite,
                ConnectionString = "Data Source=:memory:",
                HealthCheckConditions = new HealthCheckConditions()
            };

            var monitoring = new SqliteServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
