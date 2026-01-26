using Elastic.Clients.Elasticsearch;
using FluentAssertions;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using Testcontainers.Elasticsearch;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [NonParallelizable]
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
                .WithImage("docker.elastic.co/elasticsearch/elasticsearch:7.17.10")
                .WithEnvironment("discovery.type", "single-node")
                .WithEnvironment("xpack.security.enabled", "false")
                .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
                .WithWaitStrategy(Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r
                        .ForPort(9200)
                        .ForPath("/_cluster/health")
                        .ForStatusCode(System.Net.HttpStatusCode.OK)))
                .WithStartupCallback((container, ct) =>
                {
                    System.Diagnostics.Debug.WriteLine("Elasticsearch container starting...");
                    return Task.CompletedTask;
                })
                .WithCleanUp(true)
                .Build();

            await _elasticsearchContainer.StartAsync();

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
            var connectionString = _elasticsearchContainer.GetConnectionString().Replace("https", "http");
            
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

            // Create new index with 0 replicas for single-node cluster
            await client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .NumberOfReplicas(0)
                    .NumberOfShards(1)));

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
            var connectionString = _elasticsearchContainer.GetConnectionString().Replace("https", "http");
            
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
            var connectionString = _elasticsearchContainer.GetConnectionString().Replace("https", "http");
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
                EndpointOrHost = connectionString // Already converted to HTTP above
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
            var connectionString = _elasticsearchContainer.GetConnectionString().Replace("https", "http");

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
