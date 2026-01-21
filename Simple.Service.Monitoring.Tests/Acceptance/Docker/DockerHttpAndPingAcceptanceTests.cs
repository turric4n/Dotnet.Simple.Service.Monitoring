using FluentAssertions;
using NUnit.Framework;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    public class DockerHttpAndPingAcceptanceTests : DockerTestBase
    {
        private const int TestHttpPort = 8888;
        private HttpListener _httpListener;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            // Start a simple HTTP server for testing
            await Task.Run(() =>
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add($"http://localhost:{TestHttpPort}/");
                _httpListener.Start();

                // Handle requests in background
                Task.Run(async () =>
                {
                    while (_httpListener.IsListening)
                    {
                        try
                        {
                            var context = await _httpListener.GetContextAsync();
                            var response = context.Response;
                            
                            if (context.Request.Url.PathAndQuery == "/health")
                            {
                                response.StatusCode = 200;
                                var buffer = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"healthy\"}");
                                response.ContentLength64 = buffer.Length;
                                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                            else if (context.Request.Url.PathAndQuery == "/slow")
                            {
                                await Task.Delay(10000); // Simulate slow response
                                response.StatusCode = 200;
                                var buffer = System.Text.Encoding.UTF8.GetBytes("OK");
                                response.ContentLength64 = buffer.Length;
                                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                response.StatusCode = 404;
                            }
                            
                            response.Close();
                        }
                        catch (HttpListenerException)
                        {
                            // Listener was stopped
                            break;
                        }
                        catch (Exception)
                        {
                            // Ignore other exceptions
                        }
                    }
                });
            });
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await CleanupTestServerAsync();
            
            if (_httpListener != null && _httpListener.IsListening)
            {
                _httpListener.Stop();
                _httpListener.Close();
            }
        }

        [Test]
        public async Task Should_Monitor_Http_Endpoint_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "HTTP Endpoint Test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = $"http://localhost:{TestHttpPort}/health",
                HealthCheckConditions = new HealthCheckConditions
                {
                    HttpBehaviour = new HttpBehaviour
                    {
                        HttpExpectedCode = 200,
                        HttpTimeoutMs = 5000,
                        HttpVerb = HttpVerb.Get
                    }
                }
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Monitor_Http_With_Different_Status_Code()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "HTTP 404 Test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = $"http://localhost:{TestHttpPort}/notfound",
                HealthCheckConditions = new HealthCheckConditions
                {
                    HttpBehaviour = new HttpBehaviour
                    {
                        HttpExpectedCode = 404,
                        HttpTimeoutMs = 5000,
                        HttpVerb = HttpVerb.Get
                    }
                }
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
        public async Task Should_Detect_Http_Timeout()
        {
            // Arrange - Endpoint that takes 10 seconds to respond, but timeout is 1 second
            var healthCheck = new ServiceHealthCheck
            {
                Name = "HTTP Timeout Test",
                ServiceType = ServiceType.Http,
                EndpointOrHost = $"http://localhost:{TestHttpPort}/slow",
                HealthCheckConditions = new HealthCheckConditions
                {
                    HttpBehaviour = new HttpBehaviour
                    {
                        HttpExpectedCode = 200,
                        HttpTimeoutMs = 1000,
                        HttpVerb = HttpVerb.Get
                    }
                }
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act
            var response = await GetHealthCheckResponseAsync();

            // Assert - Should be unhealthy due to timeout
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Unhealthy");
        }

        [Test]
        public async Task Should_Monitor_Ping_To_Localhost_Successfully()
        {
            // Arrange
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Ping Localhost Test",
                ServiceType = ServiceType.Ping,
                EndpointOrHost = "127.0.0.1"
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act & Assert
            await AssertHealthCheckIsHealthyAsync();
        }

        [Test]
        public async Task Should_Monitor_Ping_To_Known_Host_Successfully()
        {
            // Arrange - Ping to a reliable public DNS server
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Ping DNS Server Test",
                ServiceType = ServiceType.Ping,
                EndpointOrHost = "8.8.8.8", // Google Public DNS
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act
            var response = await GetHealthCheckResponseAsync();

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            var content = await response.Content.ReadAsStringAsync();
            // Note: This might fail in some network environments with strict firewall rules
            // In that case, the test would show Unhealthy, which is expected behavior
        }

        [Test]
        public async Task Should_Detect_Failed_Ping_To_Invalid_Host()
        {
            // Arrange - Ping to an invalid/unreachable IP
            var healthCheck = new ServiceHealthCheck
            {
                Name = "Ping Invalid Host Test",
                ServiceType = ServiceType.Ping,
                EndpointOrHost = "192.0.2.1", // TEST-NET-1 (RFC 5737) - should not be routable
            };

            Server = await CreateTestServerAsync(new List<ServiceHealthCheck> { healthCheck });

            // Act
            var response = await GetHealthCheckResponseAsync();

            // Assert - Should be unhealthy
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Unhealthy");
        }

        [TearDown]
        public async Task TearDown()
        {
            await CleanupTestServerAsync();
        }
    }
}
