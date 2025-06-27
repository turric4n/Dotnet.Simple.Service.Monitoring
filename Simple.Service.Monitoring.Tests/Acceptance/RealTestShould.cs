using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Options;

namespace Simple.Service.Monitoring.Tests.Acceptance
{
    [TestFixture]
    public class RealTestShould
    {
        private TestServer _server;
        private HttpClient _client;
        private IHost _host;
        private IConfiguration _configuration;

        [SetUp]
        public void Setup()
        {
            // Create configuration
            var configValues = new Dictionary<string, string>
            {
                {"Monitoring:ServiceName", "TestService"},
                {"Monitoring:Environment", "Test"},
                {"Monitoring:BaseUrl", "http://localhost"},
                {"Monitoring:HealthChecks:Timeout", "00:00:05"}
                // Add more configuration values as needed
            };

            // Create a test host builder with the necessary services
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    config.AddInMemoryCollection(configValues);
                })
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices((hostContext, services) =>
                        {
                            // Store configuration for test access
                            _configuration = hostContext.Configuration;

                            // Add health checks

                            var monitorOptions = new MonitorOptions()
                            {
                                HealthChecks = new List<ServiceHealthCheck>()
                                {
                                    new ServiceHealthCheck()
                                    {
                                        ServiceType = ServiceType.Http,
                                        EndpointOrHost = "http://rrwerwerwrwerw2342",
                                        Name = "MyHttpHealthCheck",
                                        Alert = true,
                                        AlertBehaviour = new List<AlertBehaviour>()
                                        {
                                            new AlertBehaviour()
                                            {
                                                AlertByFailCount = 1,
                                                AlertOnce = true,
                                                TransportMethod = AlertTransportMethod.Email,
                                                TransportName = "EmailAlerting"
                                            }
                                        }
                                    },
                                    new ServiceHealthCheck()
                                    {
                                        Alert = true,
                                        ServiceType = ServiceType.Interceptor,
                                        Name = "AnotherChecksInterceptor",
                                        ExcludedInterceptionNames = new List<string>()
                                        {
                                            "MyHttpHealthCheck"
                                        },
                                        AlertBehaviour = new List<AlertBehaviour>()
                                        {
                                            new AlertBehaviour()
                                            {
                                                AlertByFailCount = 1,
                                                AlertOnce = true,
                                                TransportMethod = AlertTransportMethod.Email,
                                                TransportName = "EmailAlerting"
                                            }
                                        }
                                    }
                                },
                                EmailTransportSettings = new List<EmailTransportSettings>()
                                {
                                    new EmailTransportSettings()
                                    {
                                        Name = "EmailAlerting",
                                        Authentication = false,
                                        DisplayName = "Health",
                                        From = "hchecks@local",
                                        SmtpHost = "localhost",
                                        To = "mail@localhost"
                                    }
                                }
                            };

                            // Add service monitoring with configuration
                            services.AddServiceMonitoring(_configuration)
                                .WithRuntimeSettings(monitorOptions);

                            services.AddHealthChecks()
                                .AddCheck("test_healthy", () =>
                                    HealthCheckResult.Healthy("Test is healthy"))
                                .AddCheck("test_degraded", () => HealthCheckResult.Degraded("Test is degraded"))
                                .AddCheck("test_unhealthy", () => HealthCheckResult.Unhealthy("Test is unhealthy"));



                            // Add controllers if testing an API
                            services.AddControllers();
                        })
                        .Configure(app =>
                        {
                            // Configure the test application
                            app.UseRouting();
                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapHealthChecks("/health");
                                endpoints.MapControllers();
                            });
                        });
                });

            // Build and start the host
            _host = hostBuilder.Start();
            _server = _host.GetTestServer();
            _client = _server.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
            _host?.Dispose();
            _server?.Dispose();
        }

        [Test]
        [Explicit]
        [Category("Acceptance")]
        public async Task ExecuteHealthChecks_AndPublishResults()
        {
            // Arrange
            var healthCheckService = _host.Services.GetRequiredService<HealthCheckService>();

            // Act
            var report = await healthCheckService.CheckHealthAsync();

            // Simulate a delay to allow health checks to run
            await Task.Delay(5000);

            // Assert
            report.Status.Should().Be(HealthStatus.Unhealthy);
            report.Entries.Should().HaveCount(4);
            report.Entries.Should().ContainKeys("test_healthy", "test_degraded", "test_unhealthy", "MyHttpHealthCheck");

            report.Entries["test_healthy"].Status.Should().Be(HealthStatus.Healthy);
            report.Entries["test_degraded"].Status.Should().Be(HealthStatus.Degraded);
            report.Entries["test_unhealthy"].Status.Should().Be(HealthStatus.Unhealthy);
        }

        [Test]
        public void LoadConfiguration_Successfully()
        {
            // Assert
            _configuration.Should().NotBeNull();
            _configuration["Monitoring:ServiceName"].Should().Be("TestService");
            _configuration["Monitoring:Environment"].Should().Be("Test");
        }
    }
}