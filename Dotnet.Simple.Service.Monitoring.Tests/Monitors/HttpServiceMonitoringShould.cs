using System;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Dotnet.Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    public class HttpServiceMonitoringShould
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
        public void Given_Valid_Http_Endpoint_Monitoring_Settings()
        {
            //Arrange
            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpExpectedResponseTimeMs = 100,
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.HttpEndpoint
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Assert.DoesNotThrow(() =>
            {
                httpendpointmonitoring.SetUp();
            });
        }

        [Test]
        public void Given_InValid_Http_Endpoint_Monitoring_Settings()
        {
            //Arrange
            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpExpectedResponseTimeMs = 100,
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https:/www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.HttpEndpoint
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Assert.Throws<MalformedUriException>(() =>
            {
                httpendpointmonitoring.SetUp();
            });
        }

        [Test]
        public void Given_Valid_Multiple_Http_Endpoint_Monitoring_Settings()
        {
            //Arrange
            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpExpectedResponseTimeMs = 100,
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com,https://www.yahoo.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.HttpEndpoint
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Assert.DoesNotThrow(() =>
            {
                httpendpointmonitoring.SetUp();
            });
        }
        [Test]
        public void Given_InValid_Multiple_Http_Endpoint_Monitoring_Settings()
        {
            //Arrange
            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpExpectedResponseTimeMs = 100,
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com,http://www.yahoo.com,notanendpoint",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.HttpEndpoint
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Assert.Throws<MalformedUriException>(() =>
            {
                httpendpointmonitoring.SetUp();
            });
        }
    }
}