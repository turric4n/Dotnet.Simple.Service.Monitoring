using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture]
    [Category("Unit")]
    public class GrpcServiceMonitoringShould
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
        public void Given_Valid_Grpc_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "grpc_test",
                ServiceType = ServiceType.Grpc,
                EndpointOrHost = "https://localhost:5001",
                HealthCheckConditions = new HealthCheckConditions
                {
                    GrpcBehaviour = new GrpcBehaviour
                    {
                        UseHealthCheckProtocol = true,
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new GrpcServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Endpoint_Is_Malformed()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "grpc_test",
                ServiceType = ServiceType.Grpc,
                EndpointOrHost = "not-a-valid-uri",
                HealthCheckConditions = new HealthCheckConditions
                {
                    GrpcBehaviour = new GrpcBehaviour
                    {
                        UseHealthCheckProtocol = true
                    }
                }
            };

            var monitoring = new GrpcServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Throw_When_Endpoint_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "grpc_test",
                ServiceType = ServiceType.Grpc,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    GrpcBehaviour = new GrpcBehaviour()
                }
            };

            var monitoring = new GrpcServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Given_Http_Grpc_Endpoint()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "grpc_http",
                ServiceType = ServiceType.Grpc,
                EndpointOrHost = "http://localhost:5000",
                HealthCheckConditions = new HealthCheckConditions
                {
                    GrpcBehaviour = new GrpcBehaviour
                    {
                        UseHealthCheckProtocol = false,
                        TimeOutMs = 3000
                    }
                }
            };

            var monitoring = new GrpcServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
