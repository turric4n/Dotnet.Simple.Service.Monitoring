using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class ServiceMonitoringBase : IServiceMonitoring
    {
        protected readonly IHealthChecksBuilder HealthChecksBuilder;
        protected readonly ServiceHealthCheck HealthCheck;
        protected readonly Guid MonitorId;
        public readonly string Name;

        protected ServiceMonitoringBase(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck)
        {
            HealthChecksBuilder = healthChecksBuilder;
            HealthCheck = healthCheck;
            MonitorId = Guid.NewGuid();
            Name = healthCheck.Name;
        }

        protected internal abstract void Validate();

        public void SetUp()
        {
            Validate();
            SetMonitoring();
        }

        protected internal abstract void SetMonitoring();

        protected internal IEnumerable<string> GetTags()
        {
            var tagList = new List<string>();

            // Add service type tag in key,value format
            tagList.Add($"ServiceType,{HealthCheck.ServiceType}");

            // Add name tag
            tagList.Add($"Name,{HealthCheck.Name}");

            // Add endpoint tag if available
            if (!string.IsNullOrEmpty(HealthCheck.EndpointOrHost))
                tagList.Add($"Endpoint,{HealthCheck.EndpointOrHost}");

            // Add class name tag
            if (!string.IsNullOrEmpty(HealthCheck.FullClassName))
                tagList.Add($"ClassName,{HealthCheck.FullClassName}");

            // Add alert status
            tagList.Add($"AlertEnabled,{HealthCheck.Alert}");
            
            // Include MonitorId
            tagList.Add($"MonitorId,{MonitorId}");

            // Add additional tags if any
            if (HealthCheck.AdditionalTags != null)
            {
                foreach (var tag in HealthCheck.AdditionalTags)
                {
                    // Ensure additional tags are also in key,value format
                    // If they don't contain a comma, treat the whole string as a key with empty value
                    if (!tag.Contains(","))
                        tagList.Add($"{tag},");
                    else
                        tagList.Add(tag);
                }
            }

            return tagList;
        }

        /// <summary>
        /// Generates a comprehensive summary of the monitoring configuration
        /// including tags, alert behavior, and service-specific settings.
        /// </summary>
        /// <returns>A formatted multi-line string with the monitoring summary</returns>
        public virtual string GenerateMonitoringSummary()
        {
            var summary = new StringBuilder();

            // Basic monitor information
            summary.AppendLine($"Monitor: {HealthCheck.Name} (ID: {MonitorId})");
            summary.AppendLine($"Type: {HealthCheck.ServiceType}");

            // Endpoint/connection information
            if (!string.IsNullOrEmpty(HealthCheck.EndpointOrHost))
                summary.AppendLine($"Endpoint: {HealthCheck.EndpointOrHost}");

            // Tags
            var tags = GetTags().ToList();
            if (tags.Any())
            {
                summary.AppendLine("\nTags:");
                foreach (var tag in tags)
                {
                    summary.AppendLine($"  - {tag}");
                }
            }

            // Alert configuration
            summary.AppendLine($"\nAlerting Enabled: {HealthCheck.Alert}");
            if (HealthCheck.Alert && HealthCheck.AlertBehaviour?.Any() == true)
            {
                summary.AppendLine("\nAlert Behaviors:");
                foreach (var alertBehavior in HealthCheck.AlertBehaviour)
                {
                    summary.AppendLine($"  Transport: {alertBehavior.TransportName} ({alertBehavior.TransportMethod})");
                    summary.AppendLine($"  Alert Once: {alertBehavior.AlertOnce}");
                    summary.AppendLine($"  Alert on Recovery: {alertBehavior.AlertOnServiceRecovered}");
                    summary.AppendLine($"  Alert Frequency: Every {alertBehavior.AlertEvery}");
                    summary.AppendLine($"  Periodic/Schedule alerting enabled : {alertBehavior.UsePeriodicAlerting}");
                    summary.AppendLine($"  Schedule: {alertBehavior.StartAlertingOn} to {alertBehavior.StopAlertingOn}");
                    summary.AppendLine($"  Timezone: {alertBehavior.Timezone ?? "Default"}");
                    summary.AppendLine($"  Alert after: {alertBehavior.AlertByFailCount} consecutive failures");
                    summary.AppendLine($"  Include Environment: {alertBehavior.IncludeEnvironment}");
                    summary.AppendLine($"  Publish All Results: {alertBehavior.PublishAllResults}");
                    summary.AppendLine();
                }
            }

            return summary.ToString();
        }
    }
}
