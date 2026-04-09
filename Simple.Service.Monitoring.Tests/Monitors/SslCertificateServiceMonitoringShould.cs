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
    public class SslCertificateServiceMonitoringShould
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
        public void Given_Valid_SslCertificate_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "ssl_test",
                ServiceType = ServiceType.SslCertificate,
                EndpointOrHost = "google.com",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SslCertificateBehaviour = new SslCertificateBehaviour
                    {
                        Port = 443,
                        WarningDaysBeforeExpiry = 30,
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new SslCertificateServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Host_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "ssl_test",
                ServiceType = ServiceType.SslCertificate,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    SslCertificateBehaviour = new SslCertificateBehaviour()
                }
            };

            var monitoring = new SslCertificateServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Throw_When_Host_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "ssl_test",
                ServiceType = ServiceType.SslCertificate,
                EndpointOrHost = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SslCertificateBehaviour = new SslCertificateBehaviour()
                }
            };

            var monitoring = new SslCertificateServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Given_Custom_Port_And_Warning_Days()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "ssl_custom",
                ServiceType = ServiceType.SslCertificate,
                EndpointOrHost = "mysite.com",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SslCertificateBehaviour = new SslCertificateBehaviour
                    {
                        Port = 8443,
                        WarningDaysBeforeExpiry = 60,
                        TimeOutMs = 10000
                    }
                }
            };

            var monitoring = new SslCertificateServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
