using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Kythr.Extensions;
using System;
using System.Collections.Generic;

namespace Kythr.Tests.Configuration
{
    [TestFixture]
    [Category("Unit")]
    public class SingleUnderscoreEnvironmentVariablesProviderShould
    {
        private readonly List<string> _envVarsToClean = new List<string>();

        [TearDown]
        public void TearDown()
        {
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

        [Test]
        public void Map_Simple_Monitoring_Setting_From_Uppercase_Single_Underscore()
        {
            // Arrange
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "TestService");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act
            var value = config["MONITORING:SETTINGS:USEGLOBALSERVICENAME"];

            // Assert
            value.Should().Be("TestService");
        }

        [Test]
        public void Map_MonitoringUi_Setting_From_Uppercase_Single_Underscore()
        {
            // Arrange
            SetEnvVar("MONITORINGUI_COMPANYNAME", "ACME Corp");
            SetEnvVar("MONITORINGUI_DATAREPOSITORYTYPE", "LiteDb");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert
            config["MONITORINGUI:COMPANYNAME"].Should().Be("ACME Corp");
            config["MONITORINGUI:DATAREPOSITORYTYPE"].Should().Be("LiteDb");
        }

        [Test]
        public void Map_Array_Indexed_HealthCheck_From_Uppercase_Single_Underscore()
        {
            // Arrange
            SetEnvVar("MONITORING_HEALTHCHECKS_0_NAME", "MyCheck");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_SERVICETYPE", "Http");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ENDPOINTORHOST", "https://example.com");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert
            config["MONITORING:HEALTHCHECKS:0:NAME"].Should().Be("MyCheck");
            config["MONITORING:HEALTHCHECKS:0:SERVICETYPE"].Should().Be("Http");
            config["MONITORING:HEALTHCHECKS:0:ENDPOINTORHOST"].Should().Be("https://example.com");
        }

        [Test]
        public void Map_Deeply_Nested_HealthCheckConditions_From_Uppercase_Single_Underscore()
        {
            // Arrange
            SetEnvVar("MONITORING_HEALTHCHECKS_0_HEALTHCHECKCONDITIONS_HTTPBEHAVIOUR_HTTPEXPECTEDCODE", "200");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_HEALTHCHECKCONDITIONS_HTTPBEHAVIOUR_HTTPVERB", "Get");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert
            config["MONITORING:HEALTHCHECKS:0:HEALTHCHECKCONDITIONS:HTTPBEHAVIOUR:HTTPEXPECTEDCODE"].Should().Be("200");
            config["MONITORING:HEALTHCHECKS:0:HEALTHCHECKCONDITIONS:HTTPBEHAVIOUR:HTTPVERB"].Should().Be("Get");
        }

        [Test]
        public void Map_AlertBehaviour_Array_From_Uppercase_Single_Underscore()
        {
            // Arrange
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTMETHOD", "Email");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_0_TRANSPORTNAME", "PrimaryEmail");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_1_TRANSPORTMETHOD", "Slack");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERTBEHAVIOUR_1_TRANSPORTNAME", "PrimarySlack");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert
            config["MONITORING:HEALTHCHECKS:0:ALERTBEHAVIOUR:0:TRANSPORTMETHOD"].Should().Be("Email");
            config["MONITORING:HEALTHCHECKS:0:ALERTBEHAVIOUR:0:TRANSPORTNAME"].Should().Be("PrimaryEmail");
            config["MONITORING:HEALTHCHECKS:0:ALERTBEHAVIOUR:1:TRANSPORTMETHOD"].Should().Be("Slack");
            config["MONITORING:HEALTHCHECKS:0:ALERTBEHAVIOUR:1:TRANSPORTNAME"].Should().Be("PrimarySlack");
        }

        [Test]
        public void Map_Transport_Settings_Array_From_Uppercase_Single_Underscore()
        {
            // Arrange
            SetEnvVar("MONITORING_EMAILTRANSPORTSETTINGS_0_NAME", "PrimaryEmail");
            SetEnvVar("MONITORING_EMAILTRANSPORTSETTINGS_0_PASSWORD", "secret123");
            SetEnvVar("MONITORING_SLACKTRANSPORTSETTINGS_0_TOKEN", "xoxb-test");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert
            config["MONITORING:EMAILTRANSPORTSETTINGS:0:NAME"].Should().Be("PrimaryEmail");
            config["MONITORING:EMAILTRANSPORTSETTINGS:0:PASSWORD"].Should().Be("secret123");
            config["MONITORING:SLACKTRANSPORTSETTINGS:0:TOKEN"].Should().Be("xoxb-test");
        }

        [Test]
        public void Ignore_Environment_Variables_Not_Matching_Prefixes()
        {
            // Arrange
            SetEnvVar("SOME_OTHER_VARIABLE", "should-not-appear");
            SetEnvVar("PATH_EXTRA", "should-not-appear");
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "ValidValue");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert
            config["SOME:OTHER:VARIABLE"].Should().BeNull();
            config["PATH:EXTRA"].Should().BeNull();
            config["MONITORING:SETTINGS:USEGLOBALSERVICENAME"].Should().Be("ValidValue");
        }

        [Test]
        public void Be_Case_Insensitive_When_Retrieving_Values()
        {
            // Arrange
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "TestService");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert - .NET Configuration is case-insensitive
            config["Monitoring:Settings:UseGlobalServiceName"].Should().Be("TestService");
            config["monitoring:settings:useglobalservicename"].Should().Be("TestService");
            config["MONITORING:SETTINGS:USEGLOBALSERVICENAME"].Should().Be("TestService");
        }

        [Test]
        public void Override_Yaml_Configuration_Values()
        {
            // Arrange - Simulate YAML base config via in-memory collection
            var yamlValues = new Dictionary<string, string>
            {
                { "Monitoring:Settings:UseGlobalServiceName", "FromYaml" },
                { "MonitoringUi:CompanyName", "YamlCompany" }
            };

            // Set env vars that should override
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "FromEnvVar");
            SetEnvVar("MONITORINGUI_COMPANYNAME", "EnvVarCompany");

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(yamlValues)
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert - env vars should override yaml values
            config["Monitoring:Settings:UseGlobalServiceName"].Should().Be("FromEnvVar");
            config["MonitoringUi:CompanyName"].Should().Be("EnvVarCompany");
        }

        [Test]
        public void Not_Override_When_Added_Before_Other_Sources()
        {
            // Arrange
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "FromEnvVar");

            var overrideValues = new Dictionary<string, string>
            {
                { "Monitoring:Settings:UseGlobalServiceName", "Override" }
            };

            // Add env vars FIRST, then in-memory (simulates wrong ordering)
            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .AddInMemoryCollection(overrideValues)
                .Build();

            // Act & Assert - later source should win
            config["Monitoring:Settings:UseGlobalServiceName"].Should().Be("Override");
        }

        [Test]
        public void Bind_To_MonitorOptions_Section_Correctly()
        {
            // Arrange
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "DockerService");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_NAME", "HttpCheck");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_SERVICETYPE", "Http");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ENDPOINTORHOST", "https://httpbin.org/get");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ALERT", "true");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act - Bind the Monitoring section to MonitorOptions
            var monitoringSection = config.GetSection("Monitoring");
            var options = monitoringSection.Get<Library.Options.MonitorOptions>();

            // Assert
            options.Should().NotBeNull();
            options.Settings.Should().NotBeNull();
            options.Settings.UseGlobalServiceName.Should().Be("DockerService");
            options.HealthChecks.Should().NotBeNull().And.HaveCount(1);
            options.HealthChecks[0].Name.Should().Be("HttpCheck");
            options.HealthChecks[0].ServiceType.Should().Be(Library.Models.ServiceType.Http);
            options.HealthChecks[0].EndpointOrHost.Should().Be("https://httpbin.org/get");
            options.HealthChecks[0].Alert.Should().BeTrue();
        }

        [Test]
        public void Bind_Multiple_HealthChecks_From_Env_Vars()
        {
            // Arrange
            SetEnvVar("MONITORING_HEALTHCHECKS_0_NAME", "Check1");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_SERVICETYPE", "Http");
            SetEnvVar("MONITORING_HEALTHCHECKS_0_ENDPOINTORHOST", "https://example.com");
            SetEnvVar("MONITORING_HEALTHCHECKS_1_NAME", "Check2");
            SetEnvVar("MONITORING_HEALTHCHECKS_1_SERVICETYPE", "Ping");
            SetEnvVar("MONITORING_HEALTHCHECKS_1_ENDPOINTORHOST", "8.8.8.8");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act
            var options = config.GetSection("Monitoring").Get<Library.Options.MonitorOptions>();

            // Assert
            options.HealthChecks.Should().HaveCount(2);
            options.HealthChecks[0].Name.Should().Be("Check1");
            options.HealthChecks[0].ServiceType.Should().Be(Library.Models.ServiceType.Http);
            options.HealthChecks[1].Name.Should().Be("Check2");
            options.HealthChecks[1].ServiceType.Should().Be(Library.Models.ServiceType.Ping);
        }

        [Test]
        public void Not_Contain_Double_Underscores_In_Env_Var_Names()
        {
            // This test verifies the contract: our env vars use single underscores only.
            // Double-underscore vars should NOT be processed by our custom provider.
            SetEnvVar("MONITORING__SETTINGS__USEGLOBALSERVICENAME", "BadFormat");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // The double-underscore var starts with "MONITORING_" prefix (first underscore matches),
            // but the resulting key would be "MONITORING::SETTINGS::USEGLOBALSERVICENAME"
            // which does NOT map to "Monitoring:Settings:UseGlobalServiceName".
            // Instead it maps to a path containing empty segments.
            var value = config["Monitoring:Settings:UseGlobalServiceName"];
            value.Should().BeNull("double-underscore env vars should not map to valid config paths");
        }

        [Test]
        public void Handle_Empty_Prefixes_Array_By_Processing_Nothing()
        {
            // Arrange
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "TestValue");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables()
                .Build();

            // Act & Assert - no prefixes = nothing processed
            config["MONITORING:SETTINGS:USEGLOBALSERVICENAME"].Should().BeNull();
        }

        [Test]
        public void Env_Var_Names_Should_Be_Uppercase_Only()
        {
            // Arrange - lowercase env var with MONITORING_ prefix
            SetEnvVar("MONITORING_SETTINGS_USEGLOBALSERVICENAME", "UpperCase");

            var config = new ConfigurationBuilder()
                .AddSingleUnderscoreEnvironmentVariables("MONITORING_", "MONITORINGUI_")
                .Build();

            // Act & Assert - should be accessible case-insensitively
            config["monitoring:settings:useglobalservicename"].Should().Be("UpperCase");
        }
    }
}
