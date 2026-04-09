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
    public class AwsSqsServiceMonitoringShould
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
        public void Given_Valid_AwsSqs_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqs_test",
                ServiceType = ServiceType.AwsSqs,
                HealthCheckConditions = new HealthCheckConditions
                {
                    AwsSqsBehaviour = new AwsSqsBehaviour
                    {
                        QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
                        Region = "us-east-1",
                        AccessKey = "AKIAIOSFODNN7EXAMPLE",
                        SecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new AwsSqsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_QueueUrl_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqs_test",
                ServiceType = ServiceType.AwsSqs,
                HealthCheckConditions = new HealthCheckConditions
                {
                    AwsSqsBehaviour = new AwsSqsBehaviour
                    {
                        QueueUrl = null,
                        Region = "us-east-1"
                    }
                }
            };

            var monitoring = new AwsSqsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Throw_When_QueueUrl_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqs_test",
                ServiceType = ServiceType.AwsSqs,
                HealthCheckConditions = new HealthCheckConditions
                {
                    AwsSqsBehaviour = new AwsSqsBehaviour
                    {
                        QueueUrl = "",
                        Region = "us-east-1"
                    }
                }
            };

            var monitoring = new AwsSqsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Throw_When_Region_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqs_test",
                ServiceType = ServiceType.AwsSqs,
                HealthCheckConditions = new HealthCheckConditions
                {
                    AwsSqsBehaviour = new AwsSqsBehaviour
                    {
                        QueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/my-queue",
                        Region = null
                    }
                }
            };

            var monitoring = new AwsSqsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<Exception>();
        }

        [Test]
        public void Given_Valid_AwsSqs_With_IAM_Auth()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "sqs_iam",
                ServiceType = ServiceType.AwsSqs,
                HealthCheckConditions = new HealthCheckConditions
                {
                    AwsSqsBehaviour = new AwsSqsBehaviour
                    {
                        QueueUrl = "https://sqs.eu-west-1.amazonaws.com/987654321098/orders-queue",
                        Region = "eu-west-1",
                        TimeOutMs = 10000
                    }
                }
            };

            var monitoring = new AwsSqsServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
