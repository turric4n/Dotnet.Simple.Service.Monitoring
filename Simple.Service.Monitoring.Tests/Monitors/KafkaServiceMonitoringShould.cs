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
    public class KafkaServiceMonitoringShould
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
        public void Given_Valid_Kafka_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "kafka_test",
                ServiceType = ServiceType.Kafka,
                EndpointOrHost = "localhost:9092",
                HealthCheckConditions = new HealthCheckConditions
                {
                    KafkaBehaviour = new KafkaBehaviour
                    {
                        TopicName = "test-topic",
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new KafkaServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_BootstrapServers_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "kafka_test",
                ServiceType = ServiceType.Kafka,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    KafkaBehaviour = new KafkaBehaviour
                    {
                        TopicName = "test-topic"
                    }
                }
            };

            var monitoring = new KafkaServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Throw_When_BootstrapServers_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "kafka_test",
                ServiceType = ServiceType.Kafka,
                EndpointOrHost = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    KafkaBehaviour = new KafkaBehaviour
                    {
                        TopicName = "test-topic"
                    }
                }
            };

            var monitoring = new KafkaServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Given_Valid_Kafka_Multiple_Brokers()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "kafka_multi_broker",
                ServiceType = ServiceType.Kafka,
                EndpointOrHost = "broker1:9092,broker2:9092,broker3:9092",
                HealthCheckConditions = new HealthCheckConditions
                {
                    KafkaBehaviour = new KafkaBehaviour
                    {
                        TopicName = "health-check-topic",
                        TimeOutMs = 10000
                    }
                }
            };

            var monitoring = new KafkaServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
