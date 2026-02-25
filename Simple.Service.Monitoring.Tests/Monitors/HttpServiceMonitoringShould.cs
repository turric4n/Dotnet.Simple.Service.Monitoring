using System;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using FluentAssertions; // Added for FluentAssertions

namespace Simple.Service.Monitoring.Tests.Monitors
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
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
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
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https:/www.google.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
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
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com,https://www.yahoo.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
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
                        HttpVerb = HttpVerb.Get
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com,http://www.yahoo.com,notanendpoint",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Should_Support_Custom_Headers_In_Http_Request()
        {
            //Arrange
            var customHeaders = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Authorization", "Bearer test-token" },
                { "X-API-Key", "test-key" }
            };

            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck-with-headers",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpVerb = HttpVerb.Get,
                        CustomHttpHeaders = customHeaders
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Should_Work_Without_Custom_Headers()
        {
            //Arrange
            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck-no-headers",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpVerb = HttpVerb.Get,
                        CustomHttpHeaders = null // Explicitly null
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Should_Support_Multiple_Custom_Headers()
        {
            //Arrange
            var customHeaders = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." },
                { "X-API-Key", "sk_live_abc123" },
                { "X-Request-ID", "health-check-001" },
                { "X-Correlation-ID", "monitoring-12345" },
                { "Accept", "application/json" }
            };

            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck-multiple-headers",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpVerb = HttpVerb.Get,
                        CustomHttpHeaders = customHeaders
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Should_Support_Custom_Headers_With_Multiple_Endpoints()
        {
            //Arrange
            var customHeaders = new System.Collections.Generic.Dictionary<string, string>
            {
                { "Authorization", "Bearer shared-token" },
                { "X-Environment", "Testing" }
            };

            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck-multi-endpoint-headers",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpVerb = HttpVerb.Get,
                        CustomHttpHeaders = customHeaders
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com,https://www.bing.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Should_Support_Empty_Custom_Headers_Dictionary()
        {
            //Arrange
            var customHeaders = new System.Collections.Generic.Dictionary<string, string>();

            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck-empty-headers",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpVerb = HttpVerb.Get,
                        CustomHttpHeaders = customHeaders // Empty but not null
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Should_Support_Custom_UserAgent_Header()
        {
            //Arrange
            var customHeaders = new System.Collections.Generic.Dictionary<string, string>
            {
                { "User-Agent", "MyCompany-HealthMonitor/2.0" }
            };

            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheck-custom-useragent",
                HealthCheckConditions = new HealthCheckConditions()
                {
                    HttpBehaviour = new HttpBehaviour()
                    {
                        HttpExpectedCode = 200,
                        HttpVerb = HttpVerb.Get,
                        CustomHttpHeaders = customHeaders
                    },
                },
                AlertBehaviour = null,
                EndpointOrHost = "https://www.google.com",
                ServiceType = ServiceType.Http
            };
            //Act
            var httpendpointmonitoring = new HttpServiceMonitoring(healthChecksBuilder, httpendpointhealthcheck);
            //Assert
            Action act = () => httpendpointmonitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}