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

        public bool TimeBetweenScheduler(TimeSpan timeFrom, TimeSpan timeTo, TimeSpan currentTime)
        {
            var timeok = (currentTime.Ticks >= timeFrom.Ticks) && (currentTime.Ticks < timeTo.Ticks);
            return timeok;
        }

        public DateTime GetReportLastCheck(HealthStatus status)
        {
            var behaviour = _healthCheck
                .AlertBehaviour
                .FirstOrDefault(b => b.TransportName == _alertTransportSettings.Name);

            if (behaviour == null) return DateTime.MinValue;
            
            if (string.IsNullOrEmpty(behaviour.Timezone)) return behaviour.LastCheck;

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

            // Last status of the check always start with healthy value
            behaviour.LastStatus =
                (behaviour.LastCheck == DateTime.MinValue) 
                    ? HealthStatus.Healthy : behaviour.LastStatus;

            var failed = HealthFailed(status);
            var lastfailed = HealthFailed(behaviour.LastStatus);

            behaviour.FailedCount = failed ? 
                behaviour.FailedCount += 1 : 0;

            //Alert every
            var timeisoktoalert = TimeBetweenIsOkToAlert(behaviour.LastPublished.ToUniversalTime().TimeOfDay, 
                behaviour.AlertEvery,
                DateTime.UtcNow.TimeOfDay);
            
            //Scheduled 
            timeisoktoalert = timeisoktoalert && TimeBetweenScheduler(behaviour.StartAlertingOn, behaviour.StopAlertingOn,
                behaviour.LastPublished.ToUniversalTime().TimeOfDay);

            // Unhealthy and has to alert
            var alert = (timeisoktoalert) && (behaviour.FailedCount >= behaviour.AlertByFailCount) &&
                        (
                            // When we want to alert always
                            (failed && lastfailed && !behaviour.AlertOnce) ||
                            // When failed retries
                            (failed && lastfailed && !behaviour.LatestErrorPublished) ||
                            // Always when is time to alert and latest
                            (failed && !lastfailed)
                        );

            if (alert)
            {
                behaviour.LatestErrorPublished = true;
            }

            // On Recovered if and latest error has been published
            if (!failed && lastfailed && behaviour.AlertOnServiceRecovered && behaviour.LatestErrorPublished)
            {
                alert = true;
                behaviour.LatestErrorPublished = false;
            }

            // Alert always if we want to publish all results even checks are healthy (things like influx results in a dashboard)
            alert = (behaviour.PublishAllResults) || alert;

            return alert;
        }

        protected bool HasToPublishAlert(HealthReport report)
        {
            lock (this)
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

                behaviour.LastPublished = alert ? DateTime.Now : behaviour.LastPublished;

                if (alert)
                {
                    lock (_observers)
                    {
                        _observers.ForEach(x => x.OnNext(report));
                    }
                }

                return alert;
            }
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
