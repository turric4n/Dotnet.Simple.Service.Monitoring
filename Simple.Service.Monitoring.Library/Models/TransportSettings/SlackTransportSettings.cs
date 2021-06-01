using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class SlackTransportSettings : AlertTransportSettings
    {
        public string Token { get; set; }
        public string Channel { get; set; }
        public string Username { get; set; }
    }
}
