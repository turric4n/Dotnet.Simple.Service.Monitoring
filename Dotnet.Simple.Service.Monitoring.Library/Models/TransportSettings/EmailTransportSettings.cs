using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class EmailTransportSettings : AlertTransportSettings
    {
        public string From { get; set; }
        public string To { get; set; }
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public bool Authentication { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
