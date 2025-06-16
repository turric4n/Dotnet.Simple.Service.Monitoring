using System;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Simple.Service.Monitoring.Library;
using FluentAssertions; // Added for FluentAssertions

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    public class MysqlServiceMonitoringShould
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
        public void Given_Valid_Mysql_Host_Monitoring_Settings()
        {
            //Arrange
            var mysqlCheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                AlertBehaviour = null,
                ConnectionString = "server=127.0.0.1;uid=root;pwd=12345;database=test",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.MySql,
                HealthCheckConditions = new HealthCheckConditions()
                {
                }
            };
            //Act
            var mysqlMonitoring = new MySqlServiceMonitoring(healthChecksBuilder, mysqlCheck);
            //Assert
            Action act = () => mysqlMonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Given_Valid_Mysql_Host_And_Query_Monitoring_Settings()
        {
            //Arrange
            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                AlertBehaviour = null,
                ConnectionString = "server=127.0.0.1;uid=root;pwd=12345;database=test",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.MySql,
                HealthCheckConditions = new HealthCheckConditions()
                {
                    SqlBehaviour = new SqlBehaviour()
                    {
                        Query = "SELECT 1",
                        ExpectedResult = 1,
                        ResultExpression = ResultExpression.Equal,
                        SqlResultDataType = SqlResultDataType.Int
                    }
                }
            };
            //Act
            var mysqlMonitoring = new MySqlServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            mysqlMonitoring.SetUp();

            //Assert
            Action act = () => mysqlMonitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}