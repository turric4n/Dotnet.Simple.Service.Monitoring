using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Models
{
    public class HealthCheckData
    {
        public HealthCheckData(HealthReportEntry healthReportEntry, string name)
        {
            foreach (var tag in healthReportEntry.Tags)
            {
                var currentTag = tag.Split(",");
                if (currentTag.Length > 1)
                {
                    Tags.Add(currentTag[0], currentTag[1]);
                }
            }

            CreationDate = DateTime.Now;
            Status = (HealthStatus)healthReportEntry.Status;
            Name = name;
            LastUpdated = DateTime.Now;
            Duration = healthReportEntry.Duration.TotalMilliseconds.ToString("F2");
            Description = healthReportEntry.Description ?? "No description provided";
            CheckError = healthReportEntry.Exception != null ? healthReportEntry.Exception.Message : Description;

            ServiceType = healthReportEntry
                .Tags
                .FirstOrDefault(tag => tag.StartsWith("ServiceType,"))?.Split(",")
                .ElementAtOrDefault(1) ?? "Custom HealthCheck";
            MachineName = Environment.MachineName;
        }

        // Default constructor for serialization
        public HealthCheckData() { }
        public string Id { get; set; }
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
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }
}
