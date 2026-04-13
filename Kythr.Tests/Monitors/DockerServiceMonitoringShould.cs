using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Implementations;

namespace Kythr.Tests.Monitors
{
    [TestFixture]
    [Category("Unit")]
    public class DockerServiceMonitoringShould
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
        public void Given_Valid_Docker_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "docker_test",
                ServiceType = ServiceType.Docker,
                HealthCheckConditions = new HealthCheckConditions
                {
                    DockerBehaviour = new DockerBehaviour
                    {
                        ContainerNameOrId = "my-container",
                        DockerEndpoint = "npipe://./pipe/docker_engine",
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new DockerServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_ContainerNameOrId_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "docker_test",
                ServiceType = ServiceType.Docker,
                HealthCheckConditions = new HealthCheckConditions
                {
                    DockerBehaviour = new DockerBehaviour
                    {
                        ContainerNameOrId = null
                    }
                }
            };

            var monitoring = new DockerServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<Exception>();
        }

        [Test]
        public void Throw_When_ContainerNameOrId_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "docker_test",
                ServiceType = ServiceType.Docker,
                HealthCheckConditions = new HealthCheckConditions
                {
                    DockerBehaviour = new DockerBehaviour
                    {
                        ContainerNameOrId = ""
                    }
                }
            };

            var monitoring = new DockerServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<Exception>();
        }

        [Test]
        public void Given_Valid_Docker_With_ContainerId()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "docker_id",
                ServiceType = ServiceType.Docker,
                HealthCheckConditions = new HealthCheckConditions
                {
                    DockerBehaviour = new DockerBehaviour
                    {
                        ContainerNameOrId = "abc123def456",
                        TimeOutMs = 10000
                    }
                }
            };

            var monitoring = new DockerServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Given_Valid_Docker_With_Unix_Endpoint()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "docker_unix",
                ServiceType = ServiceType.Docker,
                HealthCheckConditions = new HealthCheckConditions
                {
                    DockerBehaviour = new DockerBehaviour
                    {
                        ContainerNameOrId = "web-server",
                        DockerEndpoint = "unix:///var/run/docker.sock",
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new DockerServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
