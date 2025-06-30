using System;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Models
{
    public class HealthCheckData
    {
        public HealthCheckData(HealthReportEntry healthReportEntry, string name)
        {
            CreationDate = DateTime.UtcNow;
            Status = healthReportEntry.Status;
            Name = name;
            LastUpdated = DateTime.UtcNow;
            Duration = healthReportEntry.Duration.Milliseconds.ToString();
            Description = healthReportEntry.Description ?? "No description provided";
            CheckError = healthReportEntry.Exception != null ? healthReportEntry.Exception.Message : "None";

            ServiceType = healthReportEntry
                .Tags
                .FirstOrDefault(tag => tag.StartsWith("ServiceType,"))?.Split(",")
                .ElementAtOrDefault(1) ?? "Custom HealthCheck";
            MachineName = Environment.MachineName;
        }

        // Default constructor for serialization
        public HealthCheckData() { }

        // Changed from fields to properties
        public DateTime CreationDate { get; set; }
        public string Name { get; set; }
        public DateTime LastUpdated { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public HealthStatus Status { get; set; }
        public string Duration { get; set; }
        public string Description { get; set; }
        public string CheckError { get; set; }
        public string ServiceType { get; set; }
        public string MachineName { get; set; }
    }
}
