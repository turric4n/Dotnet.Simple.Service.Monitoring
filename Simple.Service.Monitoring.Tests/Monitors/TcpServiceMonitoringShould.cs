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
    public class TcpServiceMonitoringShould
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
        public void Given_Valid_Tcp_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "tcp_test",
                ServiceType = ServiceType.Tcp,
                EndpointOrHost = "localhost",
                HealthCheckConditions = new HealthCheckConditions
                {
                    TcpBehaviour = new TcpBehaviour
                    {
                        Port = 8080,
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new TcpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Host_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "tcp_test",
                ServiceType = ServiceType.Tcp,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    TcpBehaviour = new TcpBehaviour
                    {
                        Port = 8080
                    }
                }
            };

            var monitoring = new TcpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Throw_When_Host_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "tcp_test",
                ServiceType = ServiceType.Tcp,
                EndpointOrHost = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    TcpBehaviour = new TcpBehaviour
                    {
                        Port = 8080
                    }
                }
            };

            var monitoring = new TcpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Throw_When_Port_Is_Zero()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "tcp_test",
                ServiceType = ServiceType.Tcp,
                EndpointOrHost = "localhost",
                HealthCheckConditions = new HealthCheckConditions
                {
                    TcpBehaviour = new TcpBehaviour
                    {
                        Port = 0
                    }
                }
            };

            var monitoring = new TcpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<Exception>();
        }

        [Test]
        public void Throw_When_Port_Is_Out_Of_Range()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "tcp_test",
                ServiceType = ServiceType.Tcp,
                EndpointOrHost = "localhost",
                HealthCheckConditions = new HealthCheckConditions
                {
                    TcpBehaviour = new TcpBehaviour
                    {
                        Port = 70000
                    }
                }
            };

            var monitoring = new TcpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<Exception>();
        }

        [Test]
        public void Given_Valid_Tcp_With_Standard_Ports()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "tcp_ssh",
                ServiceType = ServiceType.Tcp,
                EndpointOrHost = "192.168.1.1",
                HealthCheckConditions = new HealthCheckConditions
                {
                    TcpBehaviour = new TcpBehaviour
                    {
                        Port = 22,
                        TimeOutMs = 3000
                    }
                }
            };

            var monitoring = new TcpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
