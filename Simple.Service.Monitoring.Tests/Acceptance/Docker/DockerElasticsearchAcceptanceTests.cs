using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using FluentAssertions;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Testcontainers.Elasticsearch;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    public class DockerElasticsearchAcceptanceTests : DockerTestBase
    {
        private ElasticsearchContainer _elasticsearchContainer;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Create and start Elasticsearch container with PLAIN HTTP (no SSL)
            _elasticsearchContainer = new ElasticsearchBuilder()
                .WithImage("elasticsearch:8.11.0")
                .WithEnvironment("xpack.security.enabled", "false") // Disable security/SSL
                .WithEnvironment("discovery.type", "single-node")
                .WithCleanUp(true)
                .Build();

            _elasticsearchContainer.StartAsync();

            // Wait for Elasticsearch to be ready
            await Task.Delay(TimeSpan.FromSeconds(10));

            // Initialize test indices
            await InitializeElasticsearchAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();
            
            if (_elasticsearchContainer != null)
            {
                await _elasticsearchContainer.StopAsync();
                await _elasticsearchContainer.DisposeAsync();
            }
        }

        private async Task InitializeElasticsearchAsync()
        {
            // Get connection string (plain HTTP)
            var connectionString = _elasticsearchContainer.GetConnectionString();
            
            // Create client settings for PLAIN HTTP (no SSL validation needed)
            var settings = new ElasticsearchClientSettings(new Uri(connectionString));

            var client = new ElasticsearchClient(settings);

            // Verify connection
            var pingResponse = await client.PingAsync();
            pingResponse.IsValidResponse.Should().BeTrue("Elasticsearch should be reachable");

            // Create test index
            var indexName = "test-logs";
            
            // Delete index if it exists
            await client.Indices.DeleteAsync(indexName);

            // Create new index
            await client.Indices.CreateAsync(indexName);

            // Index some test documents
            await client.IndexAsync(new
            {
                timestamp = DateTime.UtcNow,
                level = "INFO",
                message = "Test log message 1"
            }, idx => idx.Index(indexName));

            await client.IndexAsync(new
            {
                timestamp = DateTime.UtcNow,
                level = "ERROR",
                message = "Test log message 2"
            }, idx => idx.Index(indexName));

            // Refresh index to make documents searchable
            await client.Indices.RefreshAsync(indexName);
        }

        [Test]
        public async Task Should_Monitor_Elasticsearch_Connection_Successfully()
        {
            // Arrange - Get plain HTTP connection string
            var connectionString = _elasticsearchContainer.GetConnectionString();
            
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Elasticsearch Test",
                ServiceType = ServiceType.ElasticSearch,
                EndpointOrHost = connectionString // Use full connection string (plain HTTP)
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Verify_Elasticsearch_Can_Search_Documents()
        {
            // Arrange - First verify we can search in Elasticsearch (plain HTTP)
            var connectionString = _elasticsearchContainer.GetConnectionString();
            var settings = new ElasticsearchClientSettings(new Uri(connectionString));

            var client = new ElasticsearchClient(settings);
            
            var searchResponse = await client.SearchAsync<object>(s => s
                .Index("test-logs"));

            searchResponse.IsValidResponse.Should().BeTrue();
            searchResponse.Documents.Count.Should().BeGreaterThan(0);

            // Now test the health check
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Elasticsearch Search Test",
                ServiceType = ServiceType.ElasticSearch,
                EndpointOrHost = connectionString
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
        public async Task Should_Monitor_Elasticsearch_Cluster_Health()
        {
            // Arrange - Use plain HTTP connection
            var connectionString = _elasticsearchContainer.GetConnectionString();

            var healthCheck = new ServiceHealthCheck
            {
                Name = "Elasticsearch Cluster Health Test",
                ServiceType = ServiceType.ElasticSearch,
                EndpointOrHost = connectionString
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Return_Unhealthy_When_Elasticsearch_Unavailable()
        {
            // Arrange - Point to non-existent Elasticsearch instance
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Elasticsearch Unavailable Test",
                ServiceType = ServiceType.ElasticSearch,
                EndpointOrHost = "http://localhost:19200" // Non-existent port
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert - Expect 503 Service Unavailable
            await AssertHealthCheckStatusAsync(System.Net.HttpStatusCode.ServiceUnavailable);
        }

        [TearDown]
        public async Task TearDown()
        {
            await CleanupTestServerAsync();
        }
    }
}
