using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Models
{
    public enum HealthStatus
    {
        /// <summary>
        /// Indicates that the health check determined that the component was unhealthy, or an unhandled
        /// exception was thrown while executing the health check.
        /// </summary>
        Unhealthy,
        /// <summary>
        /// Indicates that the health check determined that the component was in a degraded state.
        /// </summary>
        Degraded,
        /// <summary>
        /// Indicates that the health check determined that the component was healthy.
        /// </summary>
        Healthy,

        Unknown // Added for completeness, not in original enum
    }
}
