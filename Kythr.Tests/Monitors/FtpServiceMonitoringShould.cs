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
    public class FtpServiceMonitoringShould
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
        public void Given_Valid_Ftp_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "ftp_test",
                ServiceType = ServiceType.Ftp,
                EndpointOrHost = "ftp.example.com",
                HealthCheckConditions = new HealthCheckConditions
                {
                    FtpBehaviour = new FtpBehaviour
                    {
                        Port = 21,
                        UseSftp = false,
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new FtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Host_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "ftp_test",
                ServiceType = ServiceType.Ftp,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    FtpBehaviour = new FtpBehaviour()
                }
            };

            var monitoring = new FtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Throw_When_Host_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "ftp_test",
                ServiceType = ServiceType.Ftp,
                EndpointOrHost = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    FtpBehaviour = new FtpBehaviour()
                }
            };

            var monitoring = new FtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<MalformedUriException>();
        }

        [Test]
        public void Given_Valid_Sftp_With_Credentials()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sftp_test",
                ServiceType = ServiceType.Ftp,
                EndpointOrHost = "sftp.secure.com",
                HealthCheckConditions = new HealthCheckConditions
                {
                    FtpBehaviour = new FtpBehaviour
                    {
                        Port = 22,
                        UseSftp = true,
                        Username = "admin",
                        Password = "secret123",
                        TimeOutMs = 10000
                    }
                }
            };

            var monitoring = new FtpServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
