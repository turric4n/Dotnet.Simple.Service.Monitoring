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
    public class SmtpServiceMonitoringShould
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
        public void Given_Valid_Smtp_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "smtp_test",
                ServiceType = ServiceType.Smtp,
                EndpointOrHost = "smtp.gmail.com",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SmtpBehaviour = new SmtpBehaviour
                    {
                        Port = 587,
                        UseTls = true,
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new SmtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Host_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "smtp_test",
                ServiceType = ServiceType.Smtp,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    SmtpBehaviour = new SmtpBehaviour()
                }
            };

            var monitoring = new SmtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Throw_When_Host_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "smtp_test",
                ServiceType = ServiceType.Smtp,
                EndpointOrHost = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SmtpBehaviour = new SmtpBehaviour()
                }
            };

            var monitoring = new SmtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Given_Valid_Smtp_Without_Tls()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "smtp_no_tls",
                ServiceType = ServiceType.Smtp,
                EndpointOrHost = "mail.internal.local",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SmtpBehaviour = new SmtpBehaviour
                    {
                        Port = 25,
                        UseTls = false,
                        TimeOutMs = 3000
                    }
                }
            };

            var monitoring = new SmtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
