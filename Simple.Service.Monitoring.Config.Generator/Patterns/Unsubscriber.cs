using Simple.Service.Monitoring.Library.Models;

namespace Simple.Service.Monitoring.Config.Generator.Patterns
{
    public class Unsubscriber : IDisposable
    {
        private readonly List<IObserver<ServiceHealthCheck>> _observers;
        private readonly IObserver<ServiceHealthCheck> _observer;

        public Unsubscriber(List<IObserver<ServiceHealthCheck>> observers, IObserver<ServiceHealthCheck> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}
