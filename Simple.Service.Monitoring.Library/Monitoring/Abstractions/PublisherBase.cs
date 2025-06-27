using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Models;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class PublisherBase : IHealthCheckPublisher
    {
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;
        protected AlertTransportSettings _alertTransportSettings;

        protected readonly Dictionary<string, AlertBehaviour> _interceptedBehaviours;
        
        protected readonly ConcurrentBag<IReportObserver> _observers = new ConcurrentBag<IReportObserver>();

        protected PublisherBase(IHealthChecksBuilder healthChecksBuilder, 
            ServiceHealthCheck healthCheck, 
            AlertTransportSettings alertTransportSettings)
        {
            _healthChecksBuilder = healthChecksBuilder;
            _healthCheck = healthCheck;
            _alertTransportSettings = alertTransportSettings;
            _interceptedBehaviours = new Dictionary<string, AlertBehaviour>();
        }

        protected PublisherBase(IHealthChecksBuilder healthChecksBuilder)
        {
            _healthChecksBuilder = healthChecksBuilder;
        }

        private class Unsubscriber : IDisposable
        {
            private List<IReportObserver> _observers;
            private IReportObserver _observer;

            public Unsubscriber(List<IReportObserver> observers,
                IReportObserver observer)
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
            return lastAlertTime + timeToAlert <= currentTime;
        }

        public bool TimeBetweenScheduler(TimeSpan timeFrom, TimeSpan timeTo, TimeSpan currentTime)
        {
            var timeok = (currentTime.Ticks >= timeFrom.Ticks) && (currentTime.Ticks < timeTo.Ticks);
            return timeok;
        }

        public DateTime GetReportLastCheck(HealthStatus status)
        {
            var behaviour = GetAlertBehaviour();

            if (behaviour == null) return DateTime.MinValue;
            
            if (string.IsNullOrEmpty(behaviour.Timezone)) return behaviour.LastCheck;

            var timezone = TZConvert.GetTimeZoneInfo(behaviour.Timezone);

            if (timezone == null) return behaviour.LastCheck;

            var convertedTime = TimeZoneInfo.ConvertTime(behaviour.LastCheck, timezone);

            return convertedTime;
        }

        public bool ProcessOwnedAlertRules(HealthStatus status, AlertBehaviour alertBehaviour)
        {
            if (alertBehaviour == null) return false;

            // Last status of the check always start with healthy value
            alertBehaviour.LastStatus =
                (alertBehaviour.LastCheck == DateTime.MinValue) 
                    ? HealthStatus.Healthy : alertBehaviour.LastStatus;

            var failed = HealthFailed(status);
            var lastFailed = HealthFailed(alertBehaviour.LastStatus);

            alertBehaviour.FailedCount = failed ? 
                alertBehaviour.FailedCount += 1 : 0;

            //Alert every
            var timeBetweenIsOkToAlert = TimeBetweenIsOkToAlert(alertBehaviour.LastPublished.TimeOfDay, 
                alertBehaviour.AlertEvery,
                DateTime.Now.TimeOfDay);
            
            //Scheduled 
            var timeBetweenScheduler =  TimeBetweenScheduler(alertBehaviour.StartAlertingOn, alertBehaviour.StopAlertingOn,
                DateTime.Now.TimeOfDay);

            var isOkToAlert = timeBetweenIsOkToAlert && timeBetweenScheduler;

            // Unhealthy and has to alert
            var alert = (isOkToAlert) && (alertBehaviour.FailedCount >= alertBehaviour.AlertByFailCount) &&
                        (
                            // When we want to alert always
                            (failed && lastFailed && !alertBehaviour.AlertOnce) ||
                            // When failed retries
                            (failed && lastFailed && !alertBehaviour.LatestErrorPublished) ||
                            // Always when is time to alert and latest
                            (failed && !lastFailed)
                        );

            if (alert)
            {
                alertBehaviour.LatestErrorPublished = true;
            }

            // On Recovered if and latest error has been published
            if (!failed && lastFailed && alertBehaviour.AlertOnServiceRecovered && alertBehaviour.LatestErrorPublished)
            {
                alert = true;
                alertBehaviour.LatestErrorPublished = false;
            }

            // Alert always if we want to publish all results even checks are healthy (things like influx results in a dashboard)
            alert = (alertBehaviour.PublishAllResults) || alert;

            return alert;
        }

        public bool IsOkToAlert(KeyValuePair<string, HealthReportEntry> entry, bool intercepted = false)
        {
            var behaviour = intercepted ? GetInterceptedBehaviour(entry.Key, GetAlertBehaviour()) : GetAlertBehaviour();

            if (behaviour == null) return false;

            var alert = this.ProcessOwnedAlertRules(entry.Value.Status, behaviour);

            behaviour.LastStatus = entry.Value.Status;

            behaviour.LastCheck = DateTime.Now;

            behaviour.LastPublished = alert ? DateTime.Now : behaviour.LastPublished;

            //Parallel.ForEach(_observers, observer =>
            //{
            //    if (alert || observer.ExecuteAlways)
            //    {
            //        observer.OnNext(report);
            //    }
            //});

            return alert;
        }

        public KeyValuePair<string, HealthReportEntry> GetOwnedEntry(HealthReport report)
        {
            var ownedEntry = report
                .Entries
                .FirstOrDefault(x => x.Key == this._healthCheck.Name);

            return ownedEntry;
        }

        public List<KeyValuePair<string, HealthReportEntry>> GetInterceptedEntries(HealthReport report)
        {
            if (_healthCheck.ServiceType != ServiceType.Interceptor)
                return new List<KeyValuePair<string, HealthReportEntry>>();

            var excludedEntries = _healthCheck.ExcludedInterceptionNames;

            var entries = report
                .Entries
                .Where(x => !excludedEntries.Contains(x.Key))
                .ToList();

            return entries;
        }

        public abstract Task PublishAsync(HealthReport report, CancellationToken cancellationToken);
        

        protected internal abstract void Validate();

        protected internal abstract void SetPublishing();

        public void SetUp()
        {
            Validate();
            SetPublishing();
        }

        public IDisposable Subscribe(IReportObserver observer)
        {
            lock (_observers)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
                return new Unsubscriber(_observers.ToList(), observer);
            }
        }

        private AlertBehaviour GetAlertBehaviour()
        {
            return _healthCheck
                .AlertBehaviour
                .FirstOrDefault(b => b.TransportName == _alertTransportSettings.Name);
        }

        private AlertBehaviour GetInterceptedBehaviour(string healthCheckName, AlertBehaviour baseBehaviour)
        {
            var interceptedBehaviour = _interceptedBehaviours.GetValueOrDefault(healthCheckName);

            if (interceptedBehaviour == null)
            {
                interceptedBehaviour = new AlertBehaviour(baseBehaviour);
                _interceptedBehaviours.Add(healthCheckName, interceptedBehaviour);
            }

            return interceptedBehaviour;
        }
    }
}
