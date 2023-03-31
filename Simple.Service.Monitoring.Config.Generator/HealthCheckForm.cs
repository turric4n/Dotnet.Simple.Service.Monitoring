using ReaLTaiizor.Forms;
using Simple.Service.Monitoring.Library.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Simple.Service.Monitoring.Config.Generator.Patterns;

namespace Simple.Service.Monitoring.Config.Generator
{
    public partial class HealthCheckForm : LostForm, IObservable<ServiceHealthCheck>
    {
        private readonly List<IObserver<ServiceHealthCheck>> _observers;

        private ServiceHealthCheck _serviceHealthCheck;
        public HealthCheckForm()
        {
            _serviceHealthCheck = new ServiceHealthCheck();
            _observers = new List<IObserver<ServiceHealthCheck>>();
            InitializeComponent();
        }

        public IDisposable Subscribe(IObserver<ServiceHealthCheck> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
            return new Unsubscriber(_observers, observer);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            foreach (var observer in _observers.ToArray())
                if (_observers.Contains(observer))
                    observer.OnCompleted();

            _observers.Clear();
            base.OnFormClosed(e);
        }

        public void EditHealthCheck(IWin32Window owner, ServiceHealthCheck serviceHealthCheck)
        {
            _serviceHealthCheck = serviceHealthCheck;
            Show(owner);
        }

        private void lostButton1_Click(object sender, EventArgs e)
        {
            _serviceHealthCheck.EndpointOrHost = Random.Shared.Next(1, 200).ToString();
            foreach (var observer in _observers.ToArray())
                if (_observers.Contains(observer))
                    observer.OnNext(_serviceHealthCheck);
        }
    }
}
