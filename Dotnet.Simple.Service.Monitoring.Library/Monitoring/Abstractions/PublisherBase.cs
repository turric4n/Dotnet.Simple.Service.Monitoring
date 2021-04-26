using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;
using Dotnet.Simple.Service.Monitoring.Library.Models;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nest;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class PublisherBase : IHealthCheckPublisher
    {
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;
        protected DateTime lastcheck;
        protected DateTime lastpublish;
        protected HealthStatus laststatus;
        protected AlertTransportSettings _alertTransportSettings;

        protected PublisherBase(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings)
        {

            _healthChecksBuilder = healthChecksBuilder;
            _healthCheck = healthCheck;
            _alertTransportSettings = alertTransportSettings;
        }

        public bool HealthFailed(HealthStatus status)
        {
            return (status == HealthStatus.Unhealthy || status == HealthStatus.Degraded);
        }

        public bool TimeBetweenIsOkToAlert(TimeSpan lastAlertTime, TimeSpan timeToAlert, TimeSpan currentTime)
        {
            var timeok = (lastAlertTime.Ticks + timeToAlert.Ticks) <= currentTime.Ticks;
            return timeok;
        }
        public bool HasToAlert(HealthStatus status)
        {
            var failed = HealthFailed(status);
            var lastfailed = HealthFailed(laststatus);

            var behaviour = _healthCheck
                .AlertBehaviour
                .FirstOrDefault(b => b.TransportName == _alertTransportSettings.Name);

            if (behaviour == null) return false;

            var timeisoktoalert = TimeBetweenIsOkToAlert(lastcheck.ToUniversalTime().TimeOfDay, behaviour.AlertEvery,DateTime.UtcNow.TimeOfDay);

            var alert = (timeisoktoalert) &&
                            (
                                // One time
                            (failed && lastfailed && !behaviour.AlertOnce) || 
                                // Always
                            (failed && !lastfailed) || 
                                // On Recovered
                            (!failed && lastfailed && behaviour.AlertOnServiceRecovered)
                            );


            return alert;

        }

        public virtual Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var entry = report
                .Entries
                .FirstOrDefault(x => x.Key == this._healthCheck.Name);

            if (entry.Key != this._healthCheck.Name) return Task.CompletedTask;

            var alert = this.HasToAlert(entry.Value.Status);

            this.laststatus = entry.Value.Status;

            this.lastcheck = DateTime.Now;

            Condition.WithExceptionOnFailure<AlertBehaviourException>()
                .Requires(alert)
                .IsTrue();

            return Task.CompletedTask;
        }

        protected internal abstract void Validate();

        protected internal abstract void SetPublishing();

        public void SetUp()
        {
            Validate();
            SetPublishing();
        }
    }
}
