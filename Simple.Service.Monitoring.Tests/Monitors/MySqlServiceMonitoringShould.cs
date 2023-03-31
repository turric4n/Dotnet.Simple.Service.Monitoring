using System;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

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
            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                AlertBehaviour = null,
                ConnectionString = "server=127.0.0.1;uid=root;pwd=12345;database=test",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.MySql
            };
            //Act
            var mysqlMonitoring = new MySqlServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Assert.DoesNotThrow(() =>
            {
                mysqlMonitoring.SetUp();
            });
        }
    }
}