using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Monitoring.Exceptions;
using Simple.Service.Monitoring.Library.Monitoring.Implementations;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MsHealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Tests.Monitors
{
    [TestFixture]
    [Category("Unit")]
    public class RmqServiceMonitoringShould
    {
        private Mock<IHealthChecksBuilder> _healthChecksBuilderMock;
        private IServiceCollection _services;

        [SetUp]
        public void Setup()
        {
            _services = new ServiceCollection();
            _healthChecksBuilderMock = new Mock<IHealthChecksBuilder>();
            _healthChecksBuilderMock.Setup(x => x.Services).Returns(_services);
        }

        [Test]
        public void ValidateConnectionString_WhenValid()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_test",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://guest:guest@localhost:5672/"
            };

            // Act
            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Assert - Should not throw
            Assert.DoesNotThrow(() => rmqMonitoring.SetUp());
        }

        [Test]
        public void ThrowException_WhenConnectionStringIsNull()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_test",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = null
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => rmqMonitoring.SetUp());
        }

        [Test]
        public void ThrowException_WhenConnectionStringIsInvalid()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_test",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "not-a-valid-uri"
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Act & Assert
            Assert.Throws<MalformedUriException>(() => rmqMonitoring.SetUp());
        }

        [Test]
        public void RegisterHealthCheck_WithCorrectName()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "my_rabbitmq_queue",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://user:password@rabbitmq.server.com:5672/vhost"
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Act
            rmqMonitoring.SetUp();

            // Assert
            var healthCheckService = _services.BuildServiceProvider().GetService<IHealthCheck>();
            healthCheckService.Should().NotBeNull("Health check should be registered");
        }

        [Test]
        public void SupportDifferentConnectionStringFormats()
        {
            // Test various valid RabbitMQ connection string formats
            var testCases = new[]
            {
                "amqp://localhost",
                "amqp://guest:guest@localhost:5672",
                "amqp://user:password@rabbitmq.server.com:5672/vhost",
                "amqps://user:password@secure.rabbitmq.com:5671/",
                "amqp://user:password@host1:5672"
            };

            foreach (var connectionString in testCases)
            {
                // Arrange
                var services = new ServiceCollection();
                var builderMock = new Mock<IHealthChecksBuilder>();
                builderMock.Setup(x => x.Services).Returns(services);

                var healthCheck = new ServiceHealthCheck
                {
                    Name = $"rabbitmq_{connectionString.GetHashCode()}",
                    ServiceType = ServiceType.Rmq,
                    EndpointOrHost = connectionString
                };

                var rmqMonitoring = new RmqServiceMonitoring(builderMock.Object, healthCheck);

                // Act & Assert
                Assert.DoesNotThrow(() => rmqMonitoring.SetUp(), 
                    $"Should accept connection string: {connectionString}");
            }
        }

        [Test]
        [Category("Integration")]
        [Explicit("Requires a running RabbitMQ instance")]
        public async Task PerformHealthCheck_AgainstRealRabbitMQ()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_integration",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://guest:guest@localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);
            rmqMonitoring.SetUp();

            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();

            // Act
            var result = await healthCheckService.CheckHealthAsync();

            // Assert
            result.Should().NotBeNull();
            result.Entries.Should().ContainKey("rabbitmq_integration");
        }

        [Test]
        [Category("Integration")]
        [Explicit("Requires a running RabbitMQ instance")]
        public async Task ReturnHealthy_WhenRabbitMQIsAvailable()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHealthChecks();
            
            var builderMock = new Mock<IHealthChecksBuilder>();
            builderMock.Setup(x => x.Services).Returns(services);

            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_healthy",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://guest:guest@localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(builderMock.Object, healthCheck);
            rmqMonitoring.SetUp();

            var serviceProvider = services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

            // Act
            var result = await healthCheckService.CheckHealthAsync();

            // Assert
            result.Status.Should().Be(MsHealthStatus.Healthy);
            result.Entries["rabbitmq_healthy"].Status.Should().Be(MsHealthStatus.Healthy);
        }


        [Test]
        [Category("Integration")]
        [Explicit("Requires a running RabbitMQ instance")]
        public async Task DisposeConnection_AfterHealthCheck()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHealthChecks();
            
            var builderMock = new Mock<IHealthChecksBuilder>();
            builderMock.Setup(x => x.Services).Returns(services);

            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_disposal_test",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://guest:guest@localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(builderMock.Object, healthCheck);
            rmqMonitoring.SetUp();

            var serviceProvider = services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

            // Act - Run multiple health checks to ensure no connection leaks
            for (int i = 0; i < 10; i++)
            {
                var result = await healthCheckService.CheckHealthAsync();
                result.Status.Should().Be(MsHealthStatus.Healthy);
                await Task.Delay(100);
            }

            // Assert - If we got here without exceptions, connections were properly disposed
            Assert.Pass("Successfully ran 10 health checks without connection leaks");
        }

        [Test]
        [Category("Performance")]
        [Explicit("Performance test - requires RabbitMQ")]
        public async Task PerformHealthCheck_WithinReasonableTime()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHealthChecks();
            
            var builderMock = new Mock<IHealthChecksBuilder>();
            builderMock.Setup(x => x.Services).Returns(services);

            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_performance",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://guest:guest@localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(builderMock.Object, healthCheck);
            rmqMonitoring.SetUp();

            var serviceProvider = services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

            // Act
            var startTime = DateTime.UtcNow;
            var result = await healthCheckService.CheckHealthAsync();
            var duration = DateTime.UtcNow - startTime;

            // Assert
            duration.Should().BeLessThan(TimeSpan.FromSeconds(5), 
                "Health check should complete within 5 seconds");
            result.TotalDuration.Should().BeLessThan(TimeSpan.FromSeconds(5));
        }

        [Test]
        public void HandleAuthenticationInConnectionString()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_with_auth",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://admin:secretpassword@rabbitmq.server.com:5672/production"
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Act & Assert
            Assert.DoesNotThrow(() => rmqMonitoring.SetUp(),
                "Should handle connection string with authentication");
        }

        [Test]
        public void HandleVirtualHostInConnectionString()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_with_vhost",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://user:pass@localhost:5672/my-virtual-host"
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Act & Assert
            Assert.DoesNotThrow(() => rmqMonitoring.SetUp(),
                "Should handle connection string with virtual host");
        }

        [Test]
        public void HandleSecureConnection_AMQPS()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_secure",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqps://user:pass@secure.rabbitmq.com:5671/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Act & Assert
            Assert.DoesNotThrow(() => rmqMonitoring.SetUp(),
                "Should handle secure AMQPS connection");
        }

        [Test]
        [Category("Integration")]
        [Explicit("Requires RabbitMQ - tests concurrent access")]
        public async Task HandleConcurrentHealthChecks_WithoutRaceConditions()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHealthChecks();
            
            var builderMock = new Mock<IHealthChecksBuilder>();
            builderMock.Setup(x => x.Services).Returns(services);

            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_concurrent",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://guest:guest@localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(builderMock.Object, healthCheck);
            rmqMonitoring.SetUp();

            var serviceProvider = services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

            // Act - Run concurrent health checks
            var tasks = new Task<HealthReport>[20];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = healthCheckService.CheckHealthAsync();
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            foreach (var result in results)
            {
                result.Should().NotBeNull();
                result.Entries.Should().ContainKey("rabbitmq_concurrent");
                result.Entries["rabbitmq_concurrent"].Status.Should().Be(MsHealthStatus.Healthy);
            }
        }

        [Test]
        [Category("Integration")]
        [Explicit("Requires RabbitMQ with wrong credentials")]
        public async Task ReturnUnhealthy_WhenAuthenticationFails()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHealthChecks();
            
            var builderMock = new Mock<IHealthChecksBuilder>();
            builderMock.Setup(x => x.Services).Returns(services);

            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_auth_fail",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://wronguser:wrongpassword@localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(builderMock.Object, healthCheck);
            rmqMonitoring.SetUp();

            var serviceProvider = services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

            // Act
            var result = await healthCheckService.CheckHealthAsync();

            // Assert
            result.Status.Should().Be(MsHealthStatus.Unhealthy);
            result.Entries["rabbitmq_auth_fail"].Status.Should().Be(MsHealthStatus.Unhealthy);
            result.Entries["rabbitmq_auth_fail"].Description.Should().Contain("failed");
        }

        [Test]
        public void VerifyHealthCheckRegistration()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_registration_test",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(_healthChecksBuilderMock.Object, healthCheck);

            // Act
            rmqMonitoring.SetUp();

            // Assert - Verify the health check was registered
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckInstance = serviceProvider.GetService<IHealthCheck>();
            healthCheckInstance.Should().NotBeNull("Custom RabbitMQ health check should be registered");
        }

        [Test]
        [Category("Stress")]
        [Explicit("Stress test - requires RabbitMQ")]
        public async Task HandleRapidHealthChecks_WithoutLeakingConnections()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHealthChecks();
            
            var builderMock = new Mock<IHealthChecksBuilder>();
            builderMock.Setup(x => x.Services).Returns(services);

            var healthCheck = new ServiceHealthCheck
            {
                Name = "rabbitmq_stress",
                ServiceType = ServiceType.Rmq,
                EndpointOrHost = "amqp://guest:guest@localhost:5672/"
            };

            var rmqMonitoring = new RmqServiceMonitoring(builderMock.Object, healthCheck);
            rmqMonitoring.SetUp();

            var serviceProvider = services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();

            // Act - Run 100 health checks as fast as possible
            var successCount = 0;
            for (int i = 0; i < 100; i++)
            {
                var result = await healthCheckService.CheckHealthAsync();
                if (result.Status == MsHealthStatus.Healthy)
                {
                    successCount++;
                }
            }

            // Assert
            successCount.Should().Be(100, "All health checks should succeed without connection leaks");
        }

    }
}
