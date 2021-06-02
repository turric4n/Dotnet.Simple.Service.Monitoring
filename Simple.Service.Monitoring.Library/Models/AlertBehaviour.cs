﻿using System;
using System.Collections.Generic;
using System.Text;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Simple.Service.Monitoring.Library.Models
{
    public class AlertBehaviour
    {
        public AlertTransportMethod TransportMethod { get; set; }
        public string TransportName { get; set; }
        public bool AlertOnce { get; set; }
        public bool AlertOnServiceRecovered { get; set; }
        public TimeSpan AlertEvery { get; set; }
        public DateTime LastCheck { get; set; }
        public DateTime LastPublished { get; set; }
        public HealthStatus LastStatus { get; set; }

    }
}