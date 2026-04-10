using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Simple.Service.Monitoring.Extensions;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Testcontainers.Redis;

namespace Simple.Service.Monitoring.Tests.Acceptance.Docker
{
    /// <summary>
    /// Integration tests verifying that UPPERCASE single-underscore environment variables
    /// are correctly mapped through the configuration pipeline to configure real health checks.
    /// Uses Testcontainers to spin up real services.
    /// </summary>
    [TestFixture]
    [Category("Acceptance")]
    [Category("Docker")]
    [NonParallelizable]
    public class DockerEnvVarConfigurationTests
    {
        private RedisContainer _redisContainer;
        private IHost _host;
        private HttpClient _client;
        private readonly List<string> _envVarsToClean = new List<string>();

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            _redisContainer = new RedisBuilder()
                .WithCleanUp(true)
                .Build();

            await _redisContainer.StartAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_redisContainer != null)
            {
                await _redisContainer.StopAsync();
                await _redisContainer.DisposeAsync();
            }
        }

        [TearDown]
        public async Task TearDown()
        {
            _client?.Dispose();
            _client = null;

            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
                _host = null;
            }

            foreach (var key in _envVarsToClean)
            {
                Environment.SetEnvironmentVariable(key, null);
            }
            _envVarsToClean.Clear();
        }

        private void SetEnvVar(string key, string value)
        {
            Environment.SetEnvironmentVariable(key, value);
            _envVarsToClean.Add(key);
        }

        private async Task<IHost> CreateHostWithEnvVarConfigAsync()
        {
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder
                        .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_");
                })
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices((context, services) =>
                    {
                        services.AddLogging(b =>
                        {
                            b.AddConsole();
                            b.SetMinimumLevel(LogLevel.Debug);
                        });

                        services.AddServiceMonitoring(context.Configuration)
                            .WithApplicationSettings();

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

            var host = await hostBuilder.StartAsync();
            return host;
        }

        [Test]
        public async Task Should_Configure_Redis_HealthCheck_Via_Uppercase_Single_Underscore_Env_Vars()
        {
            // Arrange - Configure Redis health check entirely via UPPERCASE env vars
            var connectionString = _redisContainer.GetConnectionString();

            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "EnvVarTest");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_NAME", "RedisEnvVarCheck");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_SERVICETYPE", "Redis");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_CONNECTIONSTRING", connectionString);
            SetEnvVar("MONITORING_HEALTHCHECKS_0_HEALTHCHECKCONDITIONS_REDISBEHAVIOUR_TIMEOUTMS", "5000");

            // Act
            _host = await CreateHostWithEnvVarConfigAsync();
            _client = _host.GetTestServer().CreateClient();
            var response = await _client.GetAsync("/health");

            // Assert
            response.IsSuccessStatusCode.Should().BeTrue(
                $"Health check should be healthy. Status: {response.StatusCode}");
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Healthy");
        }

        [Test]
        public async Task Should_Override_Yaml_Settings_With_Env_Vars()
        {
            // Arrange - Set up base YAML-like config, then override with env vars
            var connectionString = _redisContainer.GetConnectionString();

            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "OverriddenByEnvVar");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_NAME", "RedisOverrideTest");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_SERVICETYPE", "Redis");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_CONNECTIONSTRING", connectionString);

            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((context, builder) =>
                {
                    // Simulate YAML base config
                    var baseConfig = new Dictionary<string, string>
                    {
                        { "Monitoring:Settings:UseGlobalServiceName", "FromYaml" }
                    };
                    builder
                        .AddInMemoryCollection(baseConfig)
                        .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_");
                })
                .ConfigureWebHost(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices((context, services) =>
                    {
                        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
                        services.AddServiceMonitoring(context.Configuration)
                            .WithApplicationSettings();
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

            // Act
            _host = await hostBuilder.StartAsync();

            // Verify the env var override worked
            var config = _host.Services.GetRequiredService<IConfiguration>();
            var globalName = config["Monitoring:Settings:UseGlobalServiceName"];

            // Assert
            globalName.Should().Be("OverriddenByEnvVar");

            _client = _host.GetTestServer().CreateClient();
            var response = await _client.GetAsync("/health");
            response.IsSuccessStatusCode.Should().BeTrue();
        }

        [Test]
        public async Task Should_Configure_Multiple_HealthChecks_Via_Env_Vars()
        {
            // Arrange - Configure two health checks via env vars
            var connectionString = _redisContainer.GetConnectionString();

            SetEnvVar("MONITORING_HEALTHCHECKS_0_NAME", "RedisCheck");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_SERVICETYPE", "Redis");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_CONNECTIONSTRING", connectionString);

            SetEnvVar("MONITORING_HEALTHCHECKS_1_NAME", "PingCheck");
            SetEnvVar("MONITORING_HEALTHCHECKS_1_SERVICETYPE", "Ping");
            SetEnvVar("MONITORING_HEALTHCHECKS_1_ENDPOINTORHOST", "127.0.0.1");

            // Act
            _host = await CreateHostWithEnvVarConfigAsync();

            // Verify options binding
            var config = _host.Services.GetRequiredService<IConfiguration>();
            var options = config.GetSection("Monitoring").Get<MonitorOptions>();

            // Assert
            options.HealthChecks.Should().HaveCount(2);
            options.HealthChecks[0].Name.Should().Be("RedisCheck");
            options.HealthChecks[0].ServiceType.Should().Be(ServiceType.Redis);
            options.HealthChecks[1].Name.Should().Be("PingCheck");
            options.HealthChecks[1].ServiceType.Should().Be(ServiceType.Ping);
        }

        [Test]
        public async Task Should_Configure_Transport_Settings_Via_Env_Vars()
        {
            // Arrange
            var connectionString = _redisContainer.GetConnectionString();

            SetEnvVar("MONITORING_HEALTHCHECKS_0_NAME", "RedisWithAlert");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_SERVICETYPE", "Redis");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_CONNECTIONSTRING", connectionString);
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERT", "true");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTMETHOD", "Console");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTNAME", "TestConsole");
            SetEnvVar("MONITORING_CONSOLETRANSPORTSETTINGS_0_NAME", "TestConsole");

            // Act
            _host = await CreateHostWithEnvVarConfigAsync();
            var config = _host.Services.GetRequiredService<IConfiguration>();
            var options = config.GetSection("Monitoring").Get<MonitorOptions>();

            // Assert
            options.HealthChecks[0].Alert.Should().BeTrue();
            options.HealthChecks[0].AlertBehaviour.Should().HaveCount(1);
            options.HealthChecks[0].AlertBehaviour[0].TransportName.Should().Be("TestConsole");
            options.ConsoleTransportSettings.Should().HaveCount(1);
            options.ConsoleTransportSettings[0].Name.Should().Be("TestConsole");
        }

        [Test]
        public async Task Should_Not_Accept_Double_Underscore_Format_For_Monitoring_Config()
        {
            // Arrange - Use double underscore format (old style) - should NOT work
            // with our custom provider since __ becomes :: (invalid path segments)
            SetEnvVar("MONITORING__HEALTHCHECKS__0__NAME", "BadFormatCheck");
            SetEnvVar("MONITORING__HEALTHCHECKS__0__SERVICETYPE", "Redis");
            SetEnvVar("MONITORING__HEALTHCHECKS__0__CONNECTIONSTRING", _redisContainer.GetConnectionString());

            // Act
            _host = await CreateHostWithEnvVarConfigAsync();
            var config = _host.Services.GetRequiredService<IConfiguration>();
            var options = config.GetSection("Monitoring").Get<MonitorOptions>();

            // Assert - double underscore vars should not create valid health checks
            // because __ maps to :: which creates empty path segments
            if (options?.HealthChecks != null && options.HealthChecks.Count > 0)
            {
                // If somehow a health check was created, it should not have the expected name
                options.HealthChecks[0].Name.Should().NotBe("BadFormatCheck",
                    "double-underscore env vars should not map correctly");
            }
        }

        [Test]
        public void Should_Verify_Env_Var_Names_Are_Uppercase_With_Single_Underscore_Only()
        {
            // This test documents and enforces the naming convention
            var validEnvVars = new[]
            {
                "MONITORING_SETTINGS_USEGLOBALSERVICENAME",
                "MONITORING_HEALTHCHECKS_0_NAME",
                "MONITORING_HEALTHCHECKS_0_SERVICETYPE",
                "MONITORING_HEALTHCHECKS_0_ENDPOINTORHOST",
                "MONITORING_HEALTHCHECKS_0_CONNECTIONSTRING",
                "MONITORING_HEALTHCHECKS_0_ALERT",
                "MONITORING_HEALTHCHECKS_0_HEALTHCHECKCONDITIONS_HTTPBEHAVIOUR_HTTPEXPECTEDCODE",
                "MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTMETHOD",
                "MONITORING_EMAILTRANSPORTSETTINGS_0_NAME",
                "MONITORING_EMAILTRANSPORTSETTINGS_0_PASSWORD",
                "MONITORINGUI_COMPANYNAME",
                "MONITORINGUI_DATAREPOSITORYTYPE",
                "MONITORINGUI_HEADERLOGOURL",
            };

            foreach (var envVar in validEnvVars)
            {
                // Must be uppercase
                envVar.Should().Be(envVar.ToUpperInvariant(),
                    $"env var '{envVar}' should be UPPERCASE");

                // Must not contain double underscores
                envVar.Should().NotContain("__",
                    $"env var '{envVar}' should use single underscores only");

                // Must only contain A-Z, 0-9, and _
                envVar.Should().MatchRegex(@"^[A-Z0-9_]+$",
                    $"env var '{envVar}' should only contain uppercase letters, digits, and underscores");
            }
        }
    }
}
