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
    public class AzureServiceBusServiceMonitoringShould
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
        public void Given_Valid_AzureServiceBus_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "asb_test",
                ServiceType = ServiceType.AzureServiceBus,
                ConnectionString = "Endpoint=sb://mybus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=abc123==",
                HealthCheckConditions = new HealthCheckConditions
                {
                    AzureServiceBusBehaviour = new AzureServiceBusBehaviour
                    {
                        QueueOrTopicName = "my-queue",
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new AzureServiceBusServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_ConnectionString_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "asb_test",
                ServiceType = ServiceType.AzureServiceBus,
                ConnectionString = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    AzureServiceBusBehaviour = new AzureServiceBusBehaviour
                    {
                        QueueOrTopicName = "my-queue"
                    }
                }
            };

            var monitoring = new AzureServiceBusServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Throw_When_ConnectionString_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "asb_test",
                ServiceType = ServiceType.AzureServiceBus,
                ConnectionString = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    AzureServiceBusBehaviour = new AzureServiceBusBehaviour
                    {
                        QueueOrTopicName = "my-queue"
                    }
                }
            };

            var monitoring = new AzureServiceBusServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Given_Valid_Topic_Subscription()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "asb_topic",
                ServiceType = ServiceType.AzureServiceBus,
                ConnectionString = "Endpoint=sb://prod.servicebus.windows.net/;SharedAccessKeyName=Listen;SharedAccessKey=xyz789==",
                HealthCheckConditions = new HealthCheckConditions
                {
                    AzureServiceBusBehaviour = new AzureServiceBusBehaviour
                    {
                        QueueOrTopicName = "events-topic",
                        TimeOutMs = 10000
                    }
                }
            };

            var monitoring = new AzureServiceBusServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
