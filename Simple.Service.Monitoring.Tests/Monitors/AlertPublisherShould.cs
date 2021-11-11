using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture(Category = "Unit")]
    public class AlertPublisherShould
    {
        private IHealthChecksBuilder healthChecksBuilder;
        private ServiceHealthCheck httpendpointhealthcheck;
        private DictionaryPublisher alertPublisher;
        private DictionaryTransportSettings alertTransportSettings;

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
                        TransportName = "Dummy",
                        TransportMethod = AlertTransportMethod.Email
                    }
                },
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.Http,
                Alert = true
            };

            alertTransportSettings = new DictionaryTransportSettings()
            {
                Name = "Dummy",
            };

            alertPublisher =
                new DictionaryPublisher(healthChecksBuilder, httpendpointhealthcheck, alertTransportSettings);
        }

        [Test]
        [TestCase("22:05:00", "00:05:00", "22:10:00")]
        [TestCase("05:00", "05:00", "10:00")]
        [TestCase("10", "1", "11")]
        public void Given_Timespan_Respect_Cooldown_Time_Between_Alerts(TimeSpan lastAlertTime, TimeSpan alertEvery,
            TimeSpan currentTime)
        {
            // Arrange
            var last = lastAlertTime;
            var delta = alertEvery;
            var current = currentTime;
            // Act
            var hasToAlert = alertPublisher.TimeBetweenIsOkToAlert(last, delta, current);
            //Assert
            Assert.IsTrue(hasToAlert);
        }

        [Test]
        [TestCase("22:05:00", "00:05:00", "22:04:00")]
        [TestCase("05:00", "05:00", "01:00")]
        [TestCase("10", "1", "08")]
        public void Given_Timespan_Does_Not_Respect_Cooldown_Time_Between_Alerts(TimeSpan lastAlertTime,
            TimeSpan alertEvery, TimeSpan currentTime)
        {
            // Arrange
            var last = lastAlertTime;
            var delta = alertEvery;
            var current = currentTime;
            // Act
            var hasToAlert = alertPublisher.TimeBetweenIsOkToAlert(last, delta, current);
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
                return alertPublisher.PublishAsync(healthReportMock, new CancellationToken());
            });
        }

        [Test]
        public void Given_Unhealthy_Check_Should_Not_Alert_During_Cooldown_But_It_Should_Alert_When_Recovered()
        {
            // Arrange
            var ok = false;

            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheckalways",
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
                        AlertEvery = TimeSpan.FromSeconds(30),
                        AlertOnServiceRecovered = true,
                        TransportName = "Dummy",
                        TransportMethod = AlertTransportMethod.Email,
                        PublishAllResults = false
                    }
                },
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var alertPublisher2 =
                new DictionaryPublisher(healthChecksBuilder, httpendpointhealthcheck, alertTransportSettings);


            var dic = new Dictionary<string, HealthReportEntry>();

            dic.Add("testhealthcheckalways", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

            var healthobserver = new Mock<IObserver<HealthReport>>();
            healthobserver.Setup(
                observer => observer.OnNext(It.IsAny<HealthReport>())).Callback(() =>
                {
                    ok = !ok;
                });

            var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

            alertPublisher2.Subscribe(healthobserver.Object);

            //Act
            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            //Assert
            Assert.IsTrue(ok);

            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            Assert.IsTrue(ok);

            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            Assert.IsTrue(ok);

            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            Assert.IsTrue(ok);

            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            Assert.IsTrue(ok);

            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            Assert.IsTrue(ok);

            dic.Clear();

            dic.Add("testhealthcheckalways", new HealthReportEntry(HealthStatus.Healthy, "", TimeSpan.Zero, null, null));

            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            Assert.IsFalse(ok);
        }

        [Test]
        public void Given_Healthy_Check_And_Then_Unhealthy_Should_Not_Alert_During_Cooldown()
        {
            // Arrange
            var ok = true;

            var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheckalways",
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
                        TransportName = "Dummy",
                        TransportMethod = AlertTransportMethod.Email,
                        PublishAllResults = false
                    }
                },
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.Http,
                Alert = true
            };

            var alertPublisher2 =
                new DictionaryPublisher(healthChecksBuilder, httpendpointhealthcheck, alertTransportSettings);


            var dic = new Dictionary<string, HealthReportEntry>();

            dic.Add("testhealthcheckalways", new HealthReportEntry(HealthStatus.Healthy, "", TimeSpan.Zero, null, null));

            var healthobserver = new Mock<IObserver<HealthReport>>();
            healthobserver.Setup(
                observer => observer.OnNext(It.IsAny<HealthReport>())).Callback(() =>
                {
                    ok = !ok;
                });

            var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

            alertPublisher2.Subscribe(healthobserver.Object);

            //Act
            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            //Assert
            Assert.IsTrue(ok);

            Thread.Sleep(4000);

            dic.Clear();

            dic.Add("testhealthcheckalways", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            Assert.IsTrue(ok);
        }

        [Test]
        public void Given_Well_Formed_Alert_Behaviour_Two_Burst_Monitors_Unhealthy_Will_Not_Alert()
        {
            var ok = false;
            // Arrange
            var dic = new Dictionary<string, HealthReportEntry>();

            dic.Add("testhealthcheck", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

            var healthobserver = new Mock<IObserver<HealthReport>>();
            healthobserver.Setup(
                observer => observer.OnNext(It.IsAny<HealthReport>())).Callback(() => { ok = !ok; });

            var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

            alertPublisher.Subscribe(healthobserver.Object);

            //Act
            alertPublisher.PublishAsync(healthReportMock, new CancellationToken());

            //Assert
            Assert.IsTrue(ok);

            //Act
            alertPublisher.PublishAsync(healthReportMock, new CancellationToken());

            //Assert
            Assert.IsTrue(ok);
        }

        [Test]
        public void Given_Well_Formed_Alert_Behaviour_Next_Follow_Unhealthy_Monitor_Will_Alert()
        {
            var ok = false;
            // Arrange
            var dic = new Dictionary<string, HealthReportEntry>();

            dic.Add("testhealthcheck", new HealthReportEntry(HealthStatus.Unhealthy, "", TimeSpan.Zero, null, null));

            var healthobserver = new Mock<IObserver<HealthReport>>();
            healthobserver.Setup(
                observer => observer.OnNext(It.IsAny<HealthReport>())).Callback(() => { ok = !ok; });

            var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

            alertPublisher.Subscribe(healthobserver.Object);

            //Act
            alertPublisher.PublishAsync(healthReportMock, new CancellationToken());

            //Assert
            Assert.IsTrue(ok);

            Thread.Sleep(TimeSpan.FromSeconds(5));

            //Act
            alertPublisher.PublishAsync(healthReportMock, new CancellationToken());

            //Assert
            Assert.IsFalse(ok);
        }

        [Test]
        public void Given_Well_Formed_Alert_Behaviour_Will_Publish_All_Results_Even_Healthy()
        {
            // Arrange
            var ok = false;
            
           var httpendpointhealthcheck = new ServiceHealthCheck()
            {
                Name = "testhealthcheckalways",
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
                        TransportName = "Dummy",
                        TransportMethod = AlertTransportMethod.Email,
                        PublishAllResults = true
                    }
                },
                EndpointOrHost = "https://www.google.com",
                MonitoringInterval = TimeSpan.FromSeconds(1),
                ServiceType = ServiceType.Http,
                Alert = true
            };

           var alertPublisher2 =
               new DictionaryPublisher(healthChecksBuilder, httpendpointhealthcheck, alertTransportSettings);


            var dic = new Dictionary<string, HealthReportEntry>();

            dic.Add("testhealthcheckalways", new HealthReportEntry(HealthStatus.Healthy, "", TimeSpan.Zero, null, null));

            var healthobserver = new Mock<IObserver<HealthReport>>();
            healthobserver.Setup(
                observer => observer.OnNext(It.IsAny<HealthReport>())).Callback(() =>
            {
                ok = !ok;
            });

            var healthReportMock = new HealthReport(dic, TimeSpan.Zero);

            alertPublisher2.Subscribe(healthobserver.Object);

            //Act
            alertPublisher2.PublishAsync(healthReportMock, new CancellationToken());

            //Assert
            Assert.IsTrue(ok);
        }
    }
}
