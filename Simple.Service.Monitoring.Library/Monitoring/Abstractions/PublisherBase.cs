using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class PublisherBase : IHealthCheckPublisher, IObservable<HealthReport>
    {
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;
        protected AlertTransportSettings _alertTransportSettings;
        protected List<IObserver<HealthReport>> _observers;

        protected PublisherBase(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _healthCheck = healthCheck;
            _alertTransportSettings = alertTransportSettings;
            _observers = new List<IObserver<HealthReport>>();
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<HealthReport>> _observers;
            private IObserver<HealthReport> _observer;

            public Unsubscriber(List<IObserver<HealthReport>> observers, IObserver<HealthReport> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
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

        public DateTime GetReportLastCheck(HealthStatus status)
        {
            var behaviour = _healthCheck
                .AlertBehaviour
                .FirstOrDefault(b => b.TransportName == _alertTransportSettings.Name);

            if (behaviour == null) return DateTime.MinValue;
            else if (string.IsNullOrEmpty(behaviour.Timezone)) return behaviour.LastCheck;

            var timezone = TZConvert.GetTimeZoneInfo(behaviour.Timezone);

            if (timezone == null) return behaviour.LastCheck;

            var convertedTime = TimeZoneInfo.ConvertTime(behaviour.LastCheck, timezone);

            return convertedTime;
        }

        public bool ProcessAlertRules(HealthStatus status)
        {
            var behaviour = _healthCheck
                .AlertBehaviour
                .FirstOrDefault(b => b.TransportName == _alertTransportSettings.Name);

            if (behaviour == null) return false;

            behaviour.LastStatus =
                (behaviour.LastCheck == DateTime.MinValue) ? HealthStatus.Healthy : behaviour.LastStatus;

            var failed = HealthFailed(status);
            var lastfailed = HealthFailed(behaviour.LastStatus);

            var timeisoktoalert = TimeBetweenIsOkToAlert(behaviour.LastCheck.ToUniversalTime().TimeOfDay, 
                behaviour.AlertEvery,
                DateTime.UtcNow.TimeOfDay);

            behaviour.LatestErrorPublished = (timeisoktoalert && failed) || behaviour.LatestErrorPublished;

            var alert = (timeisoktoalert) &&
                            (
                                // One time
                            (failed && lastfailed && !behaviour.AlertOnce) || 
                                // Always
                            (failed && !lastfailed) || 
                                // On Recovered
                            (!failed && lastfailed && behaviour.AlertOnServiceRecovered)                               
                            );

            if (behaviour.LatestErrorPublished && status == HealthStatus.Healthy)
            {
                alert = true;
                behaviour.LatestErrorPublished = false;
            }

            alert = (behaviour.PublishAllResults && timeisoktoalert) || alert;

            return alert;
        }

        protected bool HasToPublishAlert(HealthReport report)
        {
            var entry = report
                .Entries
                .FirstOrDefault(x => x.Key == this._healthCheck.Name);

            var behaviour = _healthCheck
                .AlertBehaviour
                .FirstOrDefault(b => b.TransportName == _alertTransportSettings.Name);

            if (behaviour == null) return false;

            if (entry.Key != this._healthCheck.Name) return false;

            var alert = this.ProcessAlertRules(entry.Value.Status);

            behaviour.LastStatus = entry.Value.Status;

            behaviour.LastCheck = DateTime.Now;

            behaviour.LastPublished = DateTime.Now;

            if (alert)
            {
                lock (_observers)
                {
                    _observers.ForEach(x => x.OnNext(report));
                }
            }

            return alert;
        }

        public abstract Task PublishAsync(HealthReport report, CancellationToken cancellationToken);
        

        protected internal abstract void Validate();

        protected internal abstract void SetPublishing();

        public void SetUp()
        {
            Validate();
            SetPublishing();
        }

        public IDisposable Subscribe(IObserver<HealthReport> observer)
        {
            lock (_observers)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
                return new Unsubscriber(_observers, observer);
            }
        }

    }
}
