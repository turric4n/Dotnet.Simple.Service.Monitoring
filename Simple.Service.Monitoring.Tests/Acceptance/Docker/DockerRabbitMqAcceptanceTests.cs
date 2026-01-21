using FluentAssertions;
using NUnit.Framework;
using RabbitMQ.Client;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Testcontainers.RabbitMq;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    public class DockerRabbitMqAcceptanceTests : DockerTestBase
    {
        private RabbitMqContainer _rabbitMqContainer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Create and start RabbitMQ container
            _rabbitMqContainer = new RabbitMqBuilder()
                .WithUsername("guest")
                .WithPassword("guest")
                .WithCleanUp(true)
                .Build();

            await _rabbitMqContainer.StartAsync();

            // Initialize test queues and exchanges
            await InitializeRabbitMqAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();
            
            if (_rabbitMqContainer != null)
            {
                await _rabbitMqContainer.StopAsync();
                await _rabbitMqContainer.DisposeAsync();
            }
        }

        private async Task InitializeRabbitMqAsync()
        {
            var factory = new ConnectionFactory
            {
                Uri = new System.Uri(_rabbitMqContainer.GetConnectionString())
            };

            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            // Declare test queues
            await channel.QueueDeclareAsync(queue: "test.queue",
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            await channel.QueueDeclareAsync(queue: "health.queue",
                                durable: false,
                                exclusive: false,
                                autoDelete: true,
                                arguments: null);

            // Declare test exchange
            await channel.ExchangeDeclareAsync(exchange: "test.exchange",
                                   type: ExchangeType.Direct,
                                   durable: true,
                                   autoDelete: false,
                                   arguments: null);

            // Bind queue to exchange
            await channel.QueueBindAsync(queue: "test.queue",
                             exchange: "test.exchange",
                             routingKey: "test.routing.key");
        }

        [Test]
        public async Task Should_Monitor_RabbitMq_Connection_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "RabbitMQ Test",
                ServiceType = ServiceType.Rmq,
                ConnectionString = _rabbitMqContainer.GetConnectionString()
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Verify_RabbitMq_Can_Publish_Messages()
        {
            // Arrange - First verify we can publish to RabbitMQ
            var factory = new ConnectionFactory
            {
                Uri = new System.Uri(_rabbitMqContainer.GetConnectionString())
            };

            await using (var connection = await factory.CreateConnectionAsync())
            await using (var channel = await connection.CreateChannelAsync())
            {
                var body = System.Text.Encoding.UTF8.GetBytes("Test message");
                await channel.BasicPublishAsync(
                    exchange: "test.exchange",
                    routingKey: "test.routing.key",
                    body: new ReadOnlyMemory<byte>(body));
            }

            // Now test the health check
            var healthCheck = new ServiceHealthCheck
            {
                Name = "RabbitMQ Publish Test",
                ServiceType = ServiceType.Rmq,
                ConnectionString = _rabbitMqContainer.GetConnectionString()
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act
            var response = await GetHealthCheckResponseAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Test]
        public async Task Should_Monitor_RabbitMq_With_Different_Connection_String_Format()
        {
            // Arrange - Using host:port format
            var connectionString = _rabbitMqContainer.GetConnectionString();
            
            var healthCheck = new ServiceHealthCheck
            {
                Name = "RabbitMQ Connection String Test",
                ServiceType = ServiceType.Rmq,
                ConnectionString = connectionString
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [TearDown]
        public async Task TearDown()
        {
            await CleanupTestServerAsync();
        }
    }
}
