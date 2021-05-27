using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;

namespace Dotnet.Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    public class AlertPublisherShould
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
                    new AlertBehaviour()
                    {
                        AlertEvery = TimeSpan.FromSeconds(5),
                        AlertOnServiceRecovered = true,
                        TransportName = "EmailTransport",
                        TransportMethod = AlertTransportMethod.Email
                    }
                },
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.HttpEndpoint,
                Alert = true
            };

            alertTransportSettings = new EmailTransportSettings()
            {
                Authentication = false,
                Name = "EmailTransport",
            };
            emailAlertingPublisher =
                new EmailAlertingPublisher(healthChecksBuilder, httpendpointhealthcheck, alertTransportSettings);
        }

        [Test]
        [TestCase("22:05:00", "00:05:00", "22:10:00")]
        [TestCase("05:00", "05:00", "10:00")]
        [TestCase("10", "1", "11")]
        public void Given_Timespan_Respect_Cooldown_Time_Between_Alerts(TimeSpan lastAlertTime, TimeSpan alertEvery, TimeSpan currentTime)
        {
            // Arrange
            var last = lastAlertTime;
            var delta = alertEvery;
            var current = currentTime;
            // Act
            var hasToAlert = emailAlertingPublisher.TimeBetweenIsOkToAlert(last, delta, current);
            //Assert
            Assert.IsTrue(hasToAlert);
        }

        [Test]
        [TestCase("22:05:00", "00:05:00", "22:04:00")]
        [TestCase("05:00", "05:00", "01:00")]
        [TestCase("10", "1", "08")]
        public void Given_Timespan_Does_Not_Respect_Cooldown_Time_Between_Alerts(TimeSpan lastAlertTime, TimeSpan alertEvery, TimeSpan currentTime)
        {
            // Arrange
            var last = lastAlertTime;
            var delta = alertEvery;
            var current = currentTime;
            // Act
            var hasToAlert = emailAlertingPublisher.TimeBetweenIsOkToAlert(last, delta, current);
            //Assert
            Assert.IsFalse(hasToAlert);
        }

        [Test]
        public void Given_Well_Formed_Alert_Behaviour_First_Unhealthy_Will_Alert()
        {
            // Arrange
            var dic = new Dictionary<string, HealthReportEntry>();
            dic.Add("testhealthcheck", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

            var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

            //Act
            Assert.DoesNotThrowAsync(() =>
            {
                return emailAlertingPublisher.PublishAsync(healthReportMock, new CancellationToken());
            });
        }

        //[Test]
        //public void Given_Well_Formed_Alert_Behaviour_Two_Burst_Monitors_Unhealthy_Will_Not_Alert()
        //{
        //    // Arrange
        //    var dic = new Dictionary<string, HealthReportEntry>();
        //    dic.Add("testhealthcheck", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

        //    var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

        //    //Act
        //    Assert.DoesNotThrowAsync(() =>
        //    {
        //        return emailAlertingPublisher.PublishAsync(healthReportMock, new CancellationToken());
        //    });
        //    Assert.ThrowsAsync<AlertBehaviourException>(() =>
        //    {
        //        return emailAlertingPublisher.PublishAsync(healthReportMock, new CancellationToken());
        //    });
        //}

        [Test]
        public void Given_Well_Formed_Alert_Behaviour_Next_Follow_Unhealthy_Monitor_Will_Alert()
        {
            // Arrange
            var dic = new Dictionary<string, HealthReportEntry>();
            dic.Add("testhealthcheck", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

            var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

            //Act
            Assert.DoesNotThrowAsync(() =>
            {
                return emailAlertingPublisher.PublishAsync(healthReportMock, new CancellationToken());
            });

            Thread.Sleep(TimeSpan.FromSeconds(5));
            Assert.DoesNotThrowAsync(() =>
            {
                return emailAlertingPublisher.PublishAsync(healthReportMock, new CancellationToken());
            });
        }

        //[Test]
        //public void Given_Bad_Formed_Alert_Behaviour_Next_Follow_Unhealthy_Monitor_Will_Alert()
        //{
        //    // Arrange
        //    var dic = new Dictionary<string, HealthReportEntry>();
        //    dic.Add("testhealthcheck", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

        //    var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

        //    //Act
        //    Assert.DoesNotThrowAsync(() =>
        //    {
        //        return emailAlertingPublisher.PublishAsync(healthReportMock, new CancellationToken());
        //    });

        //    Thread.Sleep(TimeSpan.FromSeconds(4));
        //    Assert.ThrowsAsync<AlertBehaviourException>(() =>
        //    {
        //        return emailAlertingPublisher.PublishAsync(healthReportMock, new CancellationToken());
        //    });
        //}
    }
}
