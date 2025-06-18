using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Simple.Service.Monitoring.UI.Models
{
    public class HealthCheckData
    {
        public HealthCheckData(HealthReportEntry healthReportEntry)
        {
            Status = healthReportEntry.Status;
            Name = healthReportEntry.Tags?.FirstOrDefault() ?? "Unknown";
            LastUpdated = DateTime.UtcNow;
            Duration = healthReportEntry.Duration.Milliseconds.ToString();
            Description = healthReportEntry.Description ?? "No description provided";
            CheckError = healthReportEntry.Exception != null ? healthReportEntry.Exception.Message : "Unknown";
        }

        // Default constructor for serialization
        public HealthCheckData() { }

        // Changed from fields to properties
        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public HealthStatus Status { get; set; }
        public string Duration { get; set; }
        public string Description { get; set; }
        public string CheckError { get; set; }
    }
}
