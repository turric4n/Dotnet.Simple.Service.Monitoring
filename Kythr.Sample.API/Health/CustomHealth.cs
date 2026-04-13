using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Kythr.Sample.API.External;

namespace Kythr.Sample.API.Health
{
    public class CustomHealth : IHealthCheck
    {
        private readonly IExternalService _externalService;

        public CustomHealth(IExternalService externalService)
        {
            _externalService = externalService;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            _externalService.DoWork();

            return Task.FromResult(HealthCheckResult.Unhealthy());
        }
    }
}
