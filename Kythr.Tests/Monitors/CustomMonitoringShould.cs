using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Implementations;
using System;
using System.Threading.Tasks;

namespace Kythr.Tests.Monitors
{
    public class CustomMonitoringShould
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
        public void Given_Valid_Custom_Monitoring_Will_Create_Instance()
        {
            //Arrange
            var custompointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                ServiceType = ServiceType.Custom
            };

            //Act
            var monitor = new CustomMonitoring(healthChecksBuilder, custompointhealthcheck);

            //Assert
            Assert.That(monitor, Is.Not.Null);
        }

        [Test]
        public void Given_Custom_Check_Function_Will_Not_Throw()
        {
            //Arrange
            var custompointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                ServiceType = ServiceType.Custom
            };

            var monitor = new CustomMonitoring(healthChecksBuilder, custompointhealthcheck);

            //Act & Assert
            Assert.DoesNotThrow(() => monitor.AddCustomCheck(() => Task.FromResult(HealthCheckResult.Healthy("OK"))));
        }
    }
}
