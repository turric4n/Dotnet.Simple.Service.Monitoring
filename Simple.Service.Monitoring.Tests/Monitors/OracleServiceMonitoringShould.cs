using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Simple.Service.Monitoring.Library;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture]
    [Category("Unit")]
    public class OracleServiceMonitoringShould
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
        public void Given_Valid_Oracle_ConnectionString_Monitoring()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "oracle_test",
                ServiceType = ServiceType.Oracle,
                ConnectionString = "Data Source=myserver:1521/orcl;User Id=myuser;Password=mypass;",
                HealthCheckConditions = new HealthCheckConditions()
            };

            var monitoring = new OracleServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Given_Valid_Oracle_EndpointOrHost_Monitoring()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "oracle_test",
                ServiceType = ServiceType.Oracle,
                EndpointOrHost = "Data Source=myserver:1521/orcl;User Id=myuser;Password=mypass;",
                HealthCheckConditions = new HealthCheckConditions()
            };

            var monitoring = new OracleServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }

        [Test]
        public void Throw_When_Both_ConnectionString_And_Endpoint_Are_Null()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "oracle_test",
                ServiceType = ServiceType.Oracle,
                ConnectionString = null,
                EndpointOrHost = null,
                HealthCheckConditions = new HealthCheckConditions()
            };

            var monitoring = new OracleServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().Throw<InvalidConnectionStringException>();
        }

        [Test]
        public void Given_Valid_Oracle_With_Custom_Query()
        {
            var healthCheck = new ServiceHealthCheck
            {
                Name = "oracle_query",
                ServiceType = ServiceType.Oracle,
                ConnectionString = "Data Source=myserver:1521/orcl;User Id=myuser;Password=mypass;",
                HealthCheckConditions = new HealthCheckConditions
                {
                    SqlBehaviour = new SqlBehaviour
                    {
                        Query = "SELECT COUNT(*) FROM v$session",
                        ExpectedResult = 1,
                        ResultExpression = ResultExpression.GreaterThan,
                        SqlResultDataType = SqlResultDataType.Int
                    }
                }
            };

            var monitoring = new OracleServiceMonitoring(_healthChecksBuilder, healthCheck);

            Action act = () => monitoring.SetUp();
            act.Should().NotThrow();
        }
    }
}
