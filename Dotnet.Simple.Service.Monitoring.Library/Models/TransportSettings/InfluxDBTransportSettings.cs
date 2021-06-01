using System;
using System.Collections.Generic;
using System.Text;

namespace Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class InfluxDbTransportSettings : AlertTransportSettings
    {
        public string Host { get; set; }
        public string Database { get; set; }
    }
}
