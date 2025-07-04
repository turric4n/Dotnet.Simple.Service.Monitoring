using System;
using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.UI.Models
{
    /// <summary>
    /// Represents a continuous time range during which a service maintained a specific health status
    /// </summary>
    public class HealthCheckTimeRange
    {
        /// <summary>
        /// Unique identifier for the time range
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Name of the service
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Machine name the service is running on
        /// </summary>
        public string MachineName { get; set; }
        
        /// <summary>
        /// When this status began
        /// </summary>
        public DateTime StartTime { get; set; }
        
        /// <summary>
        /// When this status ended (null if current)
        /// </summary>
        public DateTime? EndTime { get; set; }
        
        /// <summary>
        /// When this time range was last updated
        /// </summary>
        public DateTime UpdateTime { get; set; }
        
        /// <summary>
        /// The health status during this time range
        /// </summary>
        public HealthStatus Status { get; set; }
        
        /// <summary>
        /// Description or reason for this status
        /// </summary>
        public string StatusReason { get; set; }
        
        /// <summary>
        /// Composite key for easy grouping
        /// </summary>
        public string ServiceKey => string.IsNullOrEmpty(MachineName) 
            ? Name 
            : $"{Name} ({MachineName})";
            
        /// <summary>
        /// Indicates if this is the current (active) status
        /// </summary>
        public bool IsCurrent => !EndTime.HasValue || EndTime.Value > DateTime.Now;
        
        /// <summary>
        /// Indicates if this health check range is stale (hasn't been updated recently)
        /// </summary>
        /// <param name="threshold">The time threshold to consider a range stale</param>
        public bool IsStale(TimeSpan threshold) => 
            EndTime == null && (DateTime.Now - UpdateTime) > threshold;
    }
}