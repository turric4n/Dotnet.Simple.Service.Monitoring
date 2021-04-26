using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class PublisherBase : IHealthCheckPublisher
    {
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;
        protected TimeSpan lastcheck;
        protected TimeSpan lastpublish;
        protected HealthStatus laststatus;

        protected PublisherBase(IHealthChecksBuilder healthChecksBuilder, ServiceHealthCheck healthCheck, AlertTransportSettings alertTransportSettings)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _healthCheck = healthCheck;
        }

        public abstract Task PublishAsync(HealthReport report, CancellationToken cancellationToken);

        protected internal abstract void Validate();

        protected internal abstract void SetPublishing();

        public void SetUp()
        {
            Validate();
            SetPublishing();
        }
    }
}
