using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public interface IHealthCheckResultAnalyzer
    {
        HealthCheckResult GetHealth(object healthCheckResultData);
    }
}
