using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    /// <summary>
    /// Base class for Docker-based acceptance tests using Testcontainers.
    /// Provides common setup, teardown, and helper methods for testing health checks with real containerized services.
    /// </summary>
    public abstract class DockerTestBase
    {
        protected TestServer Server;
        protected HttpClient Client;
        protected IHost Host;

        /// <summary>
        /// Creates a test server with the specified health check configuration.
        /// </summary>
        protected async Task<TestServer> CreateTestServerAsync(List<ServiceHealthCheck> healthChecks)
        {
            var config = new Dictionary<string, string>
            {
                { "Monitoring:ServiceName", "TestService" },
                { "Monitoring:Environment", "Test" },
                { "Monitoring:BaseUrl", "http://localhost" }
            };

            var configBuilder = new ConfigurationBuilder()
                .AddInMemoryCollection(config);

            var configuration = configBuilder.Build();

            // Create MonitorOptions with the health checks
            var monitorOptions = new MonitorOptions
            {
                HealthChecks = healthChecks
            };

            var hostBuilder = new HostBuilder()
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services =>
                    {
                        services.AddSingleton<IConfiguration>(configuration);
                        
                        // Add service monitoring with runtime configuration
                        services.AddServiceMonitoring(configuration)
                            .WithRuntimeSettings(monitorOptions);

                        services.AddControllers();
                    });
                    webHost.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHealthChecks("/health");
                        });
                    });
                });

            Host = await hostBuilder.StartAsync();
            return Host.GetTestServer();
        }

        /// <summary>
        /// Calls the health check endpoint and returns the response.
        /// </summary>
        protected async Task<HttpResponseMessage> GetHealthCheckResponseAsync()
        {
            if (Client == null)
            {
                Client = Server.CreateClient();
            }
            
            return await Client.GetAsync("/health");
        }

        /// <summary>
        /// Verifies that the health check endpoint returns a healthy status.
        /// </summary>
        protected async Task AssertHealthCheckIsHealthyAsync()
        {
            var response = await GetHealthCheckResponseAsync();
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            Assert.That(content, Does.Contain("Healthy").Or.Contains("\"status\":\"Healthy\""));
        }

        /// <summary>
        /// Cleanup method to dispose of server and client resources.
        /// </summary>
        protected async Task CleanupTestServerAsync()
        {
            if (Client != null)
            {
                Client.Dispose();
                Client = null;
            }
            
            if (Host != null)
            {
                await Host.StopAsync();
                Host.Dispose();
                Host = null;
            }
            
            if (Server != null)
            {
                Server.Dispose();
                Server = null;
            }
        }

        /// <summary>
        /// Helper method to create a basic ServiceHealthCheck object.
        /// </summary>
        protected ServiceHealthCheck CreateServiceHealthCheck(
            string name,
            ServiceType serviceType,
            string connectionStringOrEndpoint = null,
            TimeSpan? monitoringInterval = null)
        {
            return new ServiceHealthCheck
            {
                Name = name,
                ServiceType = serviceType,
                ConnectionString = connectionStringOrEndpoint,
                EndpointOrHost = connectionStringOrEndpoint,
                MonitoringInterval = monitoringInterval ?? TimeSpan.FromSeconds(30)
            };
        }
    }
}
