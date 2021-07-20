using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    public class EmailAlertPublisherShould
    {
        private IHealthChecksBuilder healthChecksBuilder;
        private ServiceHealthCheck httpendpointhealthcheck;
        private EmailAlertingPublisher emailAlertingPublisher;
        private EmailTransportSettings alertTransportSettings;

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
        public void Given_Valid_Email_Settings_Should_Pass_Validation()
        {
            // Arrange
            httpendpointhealthcheck = new ServiceHealthCheck()
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
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new()
                    {
                        AlertEvery = TimeSpan.FromSeconds(5),
                        AlertOnServiceRecovered = true,
                        TransportName = "EmailTransport",
                        TransportMethod = AlertTransportMethod.Email
                    }
                },
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.Http,
                Alert = true
            };

            alertTransportSettings = new EmailTransportSettings()
            {
                Authentication = true,
                Name = "EmailTransport",
                From = "test@hotmail.com",
                SmtpHost = "test.com",
                Username = "test@hotmail.com",
                Password = "fake",
                To = "test@yahoo.com",
            };
            emailAlertingPublisher =
                new EmailAlertingPublisher(healthChecksBuilder, httpendpointhealthcheck, alertTransportSettings);

            // Assert
            Assert.DoesNotThrow(() =>
            {
                // Act
                emailAlertingPublisher.SetUp();
            });
        }

        [Test]
        public void Given_InValid_Email_Settings_Should_Pass_Validation()
        {
            httpendpointhealthcheck = new ServiceHealthCheck()
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
                AlertBehaviour = new List<AlertBehaviour>()
                {
                    new()
                    {
                        AlertEvery = TimeSpan.FromSeconds(5),
                        AlertOnServiceRecovered = true,
                        TransportName = "EmailTransport",
                        TransportMethod = AlertTransportMethod.Email
                    }
                },
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.Http,
                Alert = true
            };

            alertTransportSettings = new EmailTransportSettings()
            {
                Authentication = true,
                Name = "EmailTransport",
                From = "test@hotmail.com",
                SmtpHost = "test.com",
                Username = "test@hotmail.com",
                Password = "fake",
                To = "test@yahoo.com,oei",
            };
            emailAlertingPublisher =
                new EmailAlertingPublisher(healthChecksBuilder, httpendpointhealthcheck, alertTransportSettings);

            // Assert
            Assert.Throws<FormatException>(() =>
            {
                // Act
                emailAlertingPublisher.SetUp();
            });
        }
    }
}
