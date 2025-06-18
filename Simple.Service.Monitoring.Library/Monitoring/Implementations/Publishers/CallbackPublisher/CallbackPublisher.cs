using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Simple.Service.Monitoring.Library.Monitoring.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.CallbackPublisher
{
    public class CallbackPublisher : PublisherBase, IReportObservable
    {
        private readonly ConcurrentBag<IObserver<HealthReport>> _reportObservers = new ConcurrentBag<IObserver<HealthReport>>();

        public CallbackPublisher(IHealthChecksBuilder healthChecksBuilder) : 
            base(healthChecksBuilder)
        {
        }

        public override Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            // Notify all report observers
            foreach (var observer in _reportObservers)
            {
                try
                {
                    observer.OnNext(report);
                    
                }
                catch (Exception)
                {
                    // Swallow exceptions from observers to prevent one bad observer
                    // from breaking the entire notification process
                }
            }

            return Task.CompletedTask;
        }

        protected internal override void Validate()
        {
            // No specific validation needed for callback publisher
            return;
        }

        protected internal override void SetPublishing()
        {
            this._healthChecksBuilder.Services.AddSingleton<IHealthCheckPublisher>(sp =>
            {
                return this;
            });

            // Register this as an IReportObservable as well
            this._healthChecksBuilder.Services.AddSingleton<IReportObservable>(sp =>
            {
                return this;
            });
        }

        public IDisposable Subscribe(IObserver<HealthReport> observer)
        {
            if (observer == null)
                throw new ArgumentNullException(nameof(observer));

            if (!_reportObservers.Contains(observer))
                _reportObservers.Add(observer);

            return new Unsubscriber(_reportObservers, observer);
        }

        // Inner class for managing unsubscriptions
        private class Unsubscriber : IDisposable
        {
            private readonly ConcurrentBag<IObserver<HealthReport>> _observers;
            private readonly IObserver<HealthReport> _observer;

            public Unsubscriber(ConcurrentBag<IObserver<HealthReport>> observers, IObserver<HealthReport> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                // For ConcurrentBag, we need a different approach since we can't remove directly
                var currentObservers = _observers.ToArray();
                _observers.Clear();

                foreach (var obs in currentObservers)
                {
                    if (!obs.Equals(_observer))
                    {
                        _observers.Add(obs);
                    }
                }
            }
        }
    }
}
