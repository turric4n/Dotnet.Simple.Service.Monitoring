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
    public class MemcachedServiceMonitoringShould
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
        public void Given_Valid_Memcached_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "memcached_test",
                ServiceType = ServiceType.Memcached,
                EndpointOrHost = "localhost:11211",
                HealthCheckConditions = new HealthCheckConditions
                {
                    MemcachedBehaviour = new MemcachedBehaviour
                    {
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new MemcachedServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Endpoint_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "memcached_test",
                ServiceType = ServiceType.Memcached,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    MemcachedBehaviour = new MemcachedBehaviour()
                }
            };

            var monitoring = new MemcachedServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Throw_When_Endpoint_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "memcached_test",
                ServiceType = ServiceType.Memcached,
                EndpointOrHost = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    MemcachedBehaviour = new MemcachedBehaviour()
                }
            };

            var monitoring = new MemcachedServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Given_Valid_Memcached_With_Custom_Timeout()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "memcached_custom",
                ServiceType = ServiceType.Memcached,
                EndpointOrHost = "cache.internal:11211",
                HealthCheckConditions = new HealthCheckConditions
                {
                    MemcachedBehaviour = new MemcachedBehaviour
                    {
                        TimeOutMs = 15000
                    }
                }
            };

            var monitoring = new MemcachedServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
