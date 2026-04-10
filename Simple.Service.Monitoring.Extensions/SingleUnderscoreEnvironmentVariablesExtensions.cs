using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Simple.Service.Monitoring.Extensions
{
    /// <summary>
    /// Configuration source that reads environment variables using single underscore (_) as the
    /// hierarchy separator instead of the default double underscore (__).
    /// Variables must be UPPERCASE. Only variables matching the specified prefixes are processed.
    /// </summary>
    public class SingleUnderscoreEnvironmentVariablesConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Prefixes to match (e.g. "MONITORING_", "MONITORINGUI_").
        /// Only environment variables starting with one of these prefixes will be processed.
        /// </summary>
        public string[] Prefixes { get; set; } = Array.Empty<string>();

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SingleUnderscoreEnvironmentVariablesConfigurationProvider(Prefixes);
        }
    }

    /// <summary>
    /// Configuration provider that maps UPPERCASE_SINGLE_UNDERSCORE environment variables
    /// to the standard .NET configuration hierarchy.
    ///
    /// Example mapping:
    ///   MONITORING_HEALTHCHECKS_0_NAME → Monitoring:HealthChecks:0:Name
    ///   MONITORINGUI_COMPANYNAME       → MonitoringUi:CompanyName
    /// </summary>
    public class SingleUnderscoreEnvironmentVariablesConfigurationProvider : ConfigurationProvider
    {
        private readonly string[] _prefixes;

        public SingleUnderscoreEnvironmentVariablesConfigurationProvider(string[] prefixes)
        {
            _prefixes = prefixes ?? throw new ArgumentNullException(nameof(prefixes));
        }

        public override void Load()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var key = entry.Key?.ToString();
                if (string.IsNullOrEmpty(key))
                    continue;

                var upperKey = key.ToUpperInvariant();

                // Check if this env var matches any of our prefixes
                if (!_prefixes.Any(p => upperKey.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Replace single underscore with colon (configuration hierarchy separator)
                var configKey = key.Replace("_", ":");

                data[configKey] = entry.Value?.ToString();
            }

            Data = data;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="IConfigurationBuilder"/> to add single-underscore
    /// environment variable support.
    /// </summary>
    public static class SingleUnderscoreEnvironmentVariablesExtensions
    {
        /// <summary>
        /// Adds environment variables using single underscore (_) as the hierarchy separator.
        /// Only variables matching the specified prefixes are processed.
        /// Variables should be UPPERCASE (e.g. MONITORING_SETTINGS_EVALUATIONTIMEINSECONDS=30).
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        /// <param name="prefixes">
        /// Prefixes to match, including trailing underscore.
        /// Example: "MONITORING_", "MONITORINGUI_"
        /// </param>
        public static IConfigurationBuilder AddSingleUnderscoreEnvironmentVariables(
            this IConfigurationBuilder builder, params string[] prefixes)
        {
            builder.Add(new SingleUnderscoreEnvironmentVariablesConfigurationSource
            {
                Prefixes = prefixes
            });

            return builder;
        }
    }
}
