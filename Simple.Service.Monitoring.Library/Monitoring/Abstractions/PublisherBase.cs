// file: ..\Simple.Service.Monitoring.Library\Monitoring\Abstractions\PublisherBase.cs
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
using Microsoft.Extensions.Logging;
using TimeZoneConverter;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Simple.Service.Monitoring.Library.Monitoring.Abstractions
{
    public abstract class PublisherBase : IHealthCheckPublisher
    {
        protected readonly IHealthChecksBuilder _healthChecksBuilder;
        protected readonly ServiceHealthCheck _healthCheck;
        protected AlertTransportSettings _alertTransportSettings;
        protected readonly ILogger<IHealthCheckPublisher> _logger;

        protected readonly ConcurrentDictionary<string, AlertBehaviour> _interceptedBehaviours;
        
        // Lock objects for thread-safe behaviour state updates
        protected readonly ConcurrentDictionary<string, object> _behaviourLocks = new ConcurrentDictionary<string, object>();

        protected readonly ConcurrentBag<IObserver<KeyValuePair<string, HealthReportEntry>>> _observers =
            new ConcurrentBag<IObserver<KeyValuePair<string, HealthReportEntry>>>();

        protected PublisherBase(
            ILogger<IHealthCheckPublisher> logger,
            IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _healthChecksBuilder = healthChecksBuilder ?? throw new ArgumentNullException(nameof(healthChecksBuilder));
            _healthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _alertTransportSettings = alertTransportSettings ?? throw new ArgumentNullException(nameof(alertTransportSettings));
            _interceptedBehaviours = new ConcurrentDictionary<string, AlertBehaviour>();
        }

        protected PublisherBase(
            IHealthChecksBuilder healthChecksBuilder,
            ServiceHealthCheck healthCheck,
            AlertTransportSettings alertTransportSettings,
            ILogger<IHealthCheckPublisher> logger = null)
        {
            _healthChecksBuilder = healthChecksBuilder ?? throw new ArgumentNullException(nameof(healthChecksBuilder));
            _healthCheck = healthCheck ?? throw new ArgumentNullException(nameof(healthCheck));
            _alertTransportSettings = alertTransportSettings ?? throw new ArgumentNullException(nameof(alertTransportSettings));
            _logger = logger;
            _interceptedBehaviours = new ConcurrentDictionary<string, AlertBehaviour>();
        }

        protected PublisherBase(IHealthChecksBuilder healthChecksBuilder)
        {
            _healthChecksBuilder = healthChecksBuilder ?? throw new ArgumentNullException(nameof(healthChecksBuilder));
            _interceptedBehaviours = new ConcurrentDictionary<string, AlertBehaviour>();
        }

        private class Unsubscriber : IDisposable
        {
            private readonly ConcurrentBag<IObserver<KeyValuePair<string, HealthReportEntry>>> _observers;
            private readonly IObserver<KeyValuePair<string, HealthReportEntry>> _observer;

            public Unsubscriber(
                ConcurrentBag<IObserver<KeyValuePair<string, HealthReportEntry>>> observers,
                IObserver<KeyValuePair<string, HealthReportEntry>> observer)
            {
                _observers = observers ?? throw new ArgumentNullException(nameof(observers));
                _observer = observer ?? throw new ArgumentNullException(nameof(observer));
            }

            public void Dispose()
            {
                // Note: ConcurrentBag doesn't support removal, so this is a limitation
                // Consider using ConcurrentDictionary if removal is critical
                // For now, we'll rely on weak references or observer pattern cleanup
            }
        }

        public bool HealthFailed(HealthStatus status)
        {
            return status == HealthStatus.Unhealthy || status == HealthStatus.Degraded;
        }

        public bool TimeBetweenIsOkToAlert(TimeSpan lastAlertTime, TimeSpan timeToAlert, TimeSpan currentTime)
        {
            var nextAlertTime = lastAlertTime + timeToAlert;
            
            // Handle midnight rollover (when next alert time exceeds 24 hours)
            if (nextAlertTime.TotalHours >= 24)
            {
                nextAlertTime = TimeSpan.FromHours(nextAlertTime.TotalHours % 24);
                // After midnight rollover, check if we're past the rollover point
                // currentTime should be after nextAlertTime (after midnight) OR before lastAlertTime (still haven't wrapped)
                // But we also need to ensure the full cooldown period has passed
                if (currentTime >= nextAlertTime && currentTime < lastAlertTime)
                {
                    // We're in the rollover period (after midnight, before the original last alert time)
                    return true;
                }
                else if (currentTime >= lastAlertTime)
                {
                    // Still in the same day, haven't crossed midnight yet
                    return false;
                }
                else
                {
                    // We've crossed midnight, check if we're past the rollover point
                    return currentTime >= nextAlertTime;
                }
            }
            
            return currentTime >= nextAlertTime;
        }

        public bool TimeBetweenScheduler(TimeSpan timeFrom, TimeSpan timeTo, TimeSpan currentTime)
        {
            if (timeFrom < timeTo)
            {
                // Normal case: window within same day (e.g., 08:00 - 17:00)
                return currentTime >= timeFrom && currentTime < timeTo;
            }
            else if (timeFrom == timeTo)
            {
                // Zero duration window - only valid at the exact moment
                return currentTime == timeFrom;
            }
            else
            {
                // Window crosses midnight (e.g., 22:00 - 02:00)
                // Alert if current time is after start OR before end
                return currentTime >= timeFrom || currentTime < timeTo;
            }
        }

        public DateTime GetReportLastCheck()
        {
            var behaviour = GetAlertBehaviour();

            if (behaviour == null)
            {
                _logger?.LogWarning("Alert behaviour not found for transport: {TransportName}", _alertTransportSettings?.Name);
                return DateTime.MinValue;
            }

            if (string.IsNullOrWhiteSpace(behaviour.Timezone))
            {
                return behaviour.LastCheck;
            }

            try
            {
                var timezone = TZConvert.GetTimeZoneInfo(behaviour.Timezone);
                
                if (timezone == null)
                {
                    _logger?.LogWarning("Timezone '{Timezone}' not found. Using LastCheck without conversion.", behaviour.Timezone);
                    return behaviour.LastCheck;
                }

                return TimeZoneInfo.ConvertTime(behaviour.LastCheck, timezone);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error converting timezone for {Timezone}. Using LastCheck without conversion.", behaviour.Timezone);
                return behaviour.LastCheck;
            }
        }

        public bool ProcessOwnedAlertRules(HealthStatus status, AlertBehaviour alertBehaviour)
        {
            if (alertBehaviour == null)
            {
                return false;
            }

            InitializeAlertBehaviour(alertBehaviour);

            var failed = HealthFailed(status);
            var lastFailed = HealthFailed((HealthStatus)alertBehaviour.LastStatus);

            UpdateFailedCount(alertBehaviour, failed);

            if (alertBehaviour.PublishAllResults)
            {
                return true;
            }

            var isOkToAlert = IsWithinAlertingWindow(alertBehaviour);

            var shouldAlert = ShouldTriggerAlert(failed, lastFailed, alertBehaviour, isOkToAlert);

            if (shouldAlert)
            {
                alertBehaviour.LatestErrorPublished = true;
            }

            var shouldAlertOnRecovery = ShouldAlertOnRecovery(failed, lastFailed, alertBehaviour);

            if (shouldAlertOnRecovery)
            {
                shouldAlert = true;
                alertBehaviour.LatestErrorPublished = false;
            }

            return shouldAlert;
        }

        private void InitializeAlertBehaviour(AlertBehaviour alertBehaviour)
        {
            if (alertBehaviour.LastCheck == DateTime.MinValue)
            {
                alertBehaviour.LastStatus = (Models.HealthStatus)HealthStatus.Healthy;
            }
        }

        private void UpdateFailedCount(AlertBehaviour alertBehaviour, bool failed)
        {
            if (alertBehaviour == null) return;
            
            var lockKey = alertBehaviour.TransportName ?? "default";
            var lockObj = _behaviourLocks.GetOrAdd(lockKey, _ => new object());
            
            lock (lockObj)
            {
                if (failed)
                {
                    alertBehaviour.FailedCount += 1;
                }
                else
                {
                    alertBehaviour.FailedCount = 0;
                }
            }
        }

        private bool IsWithinAlertingWindow(AlertBehaviour alertBehaviour)
        {
            var currentTime = DateTime.Now.TimeOfDay;

            var timeBetweenIsOkToAlert = TimeBetweenIsOkToAlert(
                alertBehaviour.LastPublished.TimeOfDay,
                alertBehaviour.AlertEvery,
                currentTime);

            var timeBetweenScheduler = TimeBetweenScheduler(
                alertBehaviour.StartAlertingOn,
                alertBehaviour.StopAlertingOn,
                currentTime);

            return timeBetweenIsOkToAlert && timeBetweenScheduler;
        }

        private bool ShouldTriggerAlert(bool failed, bool lastFailed, AlertBehaviour alertBehaviour, bool isOkToAlert)
        {
            if (!isOkToAlert || alertBehaviour.FailedCount < alertBehaviour.AlertByFailCount)
            {
                return false;
            }

            // Alert on continuous failures (when AlertOnce is false)
            if (failed && lastFailed && !alertBehaviour.AlertOnce)
            {
                return true;
            }

            // Alert on failures that haven't been published yet
            if (failed && lastFailed && !alertBehaviour.LatestErrorPublished)
            {
                return true;
            }

            // Alert on new failures (status changed to failed)
            if (failed && !lastFailed)
            {
                return true;
            }

            return false;
        }

        private bool ShouldAlertOnRecovery(bool failed, bool lastFailed, AlertBehaviour alertBehaviour)
        {
            return !failed
                && lastFailed
                && alertBehaviour.AlertOnServiceRecovered
                && alertBehaviour.LatestErrorPublished;
        }

        public bool IsOkToAlert(KeyValuePair<string, HealthReportEntry> entry, bool intercepted = false)
        {
            if (default(KeyValuePair<string, HealthReportEntry>).Equals(entry))
            {
                return false;
            }

            var behaviour = intercepted
                ? GetInterceptedBehaviour(entry.Key, GetAlertBehaviour())
                : GetAlertBehaviour();

            if (behaviour == null)
            {
                return false;
            }

            var alert = ProcessOwnedAlertRules(entry.Value.Status, behaviour);

            UpdateBehaviourState(behaviour, entry, alert);

            return alert;
        }

        private void UpdateBehaviourState(
            AlertBehaviour behaviour,
            KeyValuePair<string, HealthReportEntry> entry,
            bool alert)
        {
            if (behaviour == null) return;
            
            // Get or create a lock object for this specific behaviour to ensure thread-safe updates
            var lockKey = behaviour.TransportName ?? "default";
            var lockObj = _behaviourLocks.GetOrAdd(lockKey, _ => new object());
            
            lock (lockObj)
            {
                behaviour.LastStatus = (Models.HealthStatus)entry.Value.Status;
                behaviour.LastCheck = DateTime.Now;

                if (alert)
                {
                    behaviour.LastPublished = DateTime.Now;
                }
            }
        }

        public void AlertObservers(KeyValuePair<string, HealthReportEntry> entry)
        {
            // Create a snapshot of observers to avoid locking during iteration
            var observerSnapshot = _observers.ToArray();

            Parallel.ForEach(observerSnapshot, observer =>
            {
                try
                {
                    observer.OnNext(entry);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error notifying observer for health check: {HealthCheckName}", entry.Key);
                }
            });
        }

        public KeyValuePair<string, HealthReportEntry> GetOwnedEntry(HealthReport report)
        {
            if (report?.Entries == null)
            {
                return default;
            }

            return report.Entries.FirstOrDefault(x => x.Key == _healthCheck?.Name);
        }

        public List<KeyValuePair<string, HealthReportEntry>> GetInterceptedEntries(HealthReport report)
        {
            if (report?.Entries == null || _healthCheck == null)
            {
                return new List<KeyValuePair<string, HealthReportEntry>>();
            }

            if (_healthCheck.ServiceType != ServiceType.Interceptor)
            {
                return new List<KeyValuePair<string, HealthReportEntry>>();
            }

            var excludedEntries = _healthCheck.ExcludedInterceptionNames ?? new List<string>();

            return report.Entries
                .Where(x => !excludedEntries.Contains(x.Key))
                .ToList();
        }

        public abstract Task PublishAsync(HealthReport report, CancellationToken cancellationToken);

        protected internal abstract void Validate();

        protected internal abstract void SetPublishing();

        public void SetUp()
        {
            Validate();
            SetPublishing();
        }

        public IDisposable Subscribe(IObserver<KeyValuePair<string, HealthReportEntry>> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }

            return new Unsubscriber(_observers, observer);
        }

        private AlertBehaviour GetAlertBehaviour()
        {
            if (_healthCheck?.AlertBehaviour == null || _alertTransportSettings == null)
            {
                return null;
            }

            return _healthCheck.AlertBehaviour
                .FirstOrDefault(b => b.TransportName == _alertTransportSettings.Name);
        }

        private AlertBehaviour GetInterceptedBehaviour(string healthCheckName, AlertBehaviour baseBehaviour)
        {
            if (baseBehaviour == null)
            {
                return null;
            }

            return _interceptedBehaviours.GetOrAdd(healthCheckName, key => new AlertBehaviour(baseBehaviour));
        }
    }
}