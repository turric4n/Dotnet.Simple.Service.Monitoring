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
    public class MongoDbServiceMonitoringShould
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
        public void Given_Valid_MongoDb_Monitoring_Settings()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "mongodb_test",
                ServiceType = ServiceType.MongoDb,
                ConnectionString = "mongodb://localhost:27017",
                HealthCheckConditions = new HealthCheckConditions
                {
                    MongoDbBehaviour = new MongoDbBehaviour
                    {
                        DatabaseName = "testdb",
                        TimeOutMs = 5000
                    }
                }
            };

            var monitoring = new MongoDbServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_ConnectionString_Is_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "mongodb_test",
                ServiceType = ServiceType.MongoDb,
                ConnectionString = null,
                HealthCheckConditions = new HealthCheckConditions
                {
                    MongoDbBehaviour = new MongoDbBehaviour
                    {
                        DatabaseName = "testdb"
                    }
                }
            };

            var monitoring = new MongoDbServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Throw_When_ConnectionString_Is_Empty()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "mongodb_test",
                ServiceType = ServiceType.MongoDb,
                ConnectionString = "",
                HealthCheckConditions = new HealthCheckConditions
                {
                    MongoDbBehaviour = new MongoDbBehaviour
                    {
                        DatabaseName = "testdb"
                    }
                }
            };

            var monitoring = new MongoDbServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Register_HealthCheck_With_Correct_Name()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "my_mongodb_check",
                ServiceType = ServiceType.MongoDb,
                ConnectionString = "mongodb://user:pass@host1:27017,host2:27017/admin?replicaSet=rs0",
                HealthCheckConditions = new HealthCheckConditions
                {
                    MongoDbBehaviour = new MongoDbBehaviour
                    {
                        DatabaseName = "production_db",
                        TimeOutMs = 10000
                    }
                }
            };

            var services = new ServiceCollection();
            var builder = services.AddHealthChecks();
            var monitoring = new MongoDbServiceMonitoring(builder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
