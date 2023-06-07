using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using System;
using System.Collections.Generic;
using Simple.Service.Monitoring.Library;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    public class SqlServerMonitoringShould
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
        public void Given_Valid_SQl_Host_Monitoring_Settings()
        {
            //Arrange
            var sqlHealthCheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                AlertBehaviour = new List<AlertBehaviour>()
                {

                },
                ConnectionString = "server=127.0.0.1;uid=root;pwd=12345;database=test",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.MsSql,
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
            var sqlMonitoring = new MsSqlServiceMonitoring(healthChecksBuilder, sqlHealthCheck);
            //Assert
            Assert.DoesNotThrow(() =>
            {
                sqlMonitoring.SetUp();
            });
        }
    }
}
