using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Kythr.Library.Models;
using Kythr.Library.Monitoring.Exceptions;
using Kythr.Library.Monitoring.Implementations;

namespace Kythr.Tests.Monitors
{
    [TestFixture]
    [Category("Unit")]
    public class DnsServiceMonitoringShould
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
        public void Given_Valid_Dns_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "dns_test",
                ServiceType = ServiceType.Dns,
                EndpointOrHost = "google.com",
                HealthCheckConditions = new HealthCheckConditions
                {
                    DnsBehaviour = new DnsBehaviour
                    {
                        ExpectedIpAddress = "142.250.80.46",
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new DnsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Hostname_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "dns_test",
                ServiceType = ServiceType.Dns,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    DnsBehaviour = new DnsBehaviour
                    {
                        ExpectedIpAddress = "1.2.3.4"
                    }
                }
            };

            var monitoring = new DnsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Throw_When_Hostname_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "dns_test",
                ServiceType = ServiceType.Dns,
                EndpointOrHost = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    DnsBehaviour = new DnsBehaviour()
                }
            };

            var monitoring = new DnsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Given_Valid_Dns_Without_ExpectedIp()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "dns_resolve_only",
                ServiceType = ServiceType.Dns,
                EndpointOrHost = "example.com",
                HealthCheckConditions = new HealthCheckConditions
                {
                    DnsBehaviour = new DnsBehaviour
                    {
                        TimeOutMs = 3000
                    }
                }
            };

            var monitoring = new DnsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
