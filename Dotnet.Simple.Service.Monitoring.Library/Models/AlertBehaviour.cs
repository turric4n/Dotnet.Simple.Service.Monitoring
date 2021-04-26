using System;
using System.Collections.Generic;
using System.Text;
using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;

namespace Dotnet.Simple.Service.Monitoring.Library.Models
{
    public class AlertBehaviour
    {
        public AlertTransportMethod TransportMethod { get; set; }
        public string TransportName { get; set; }
        public bool AlertOnce { get; set; }
        public bool AlertOnServiceRecovered { get; set; }
        public TimeSpan StartAlertingOn { get; set; }
        public TimeSpan StopAlertingOn { get; set; }
        public TimeSpan AlertEvery { get; set; }
        public TimeSpan AlertOn { get; set; }
    }
}
