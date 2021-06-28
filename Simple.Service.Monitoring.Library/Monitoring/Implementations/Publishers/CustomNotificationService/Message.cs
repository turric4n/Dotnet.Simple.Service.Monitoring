using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.CustomNotificationService
{
    public class Message
    {
        public string ProjectName { get; set; }
        public string Environment { get; set; }
        public string Msg { get; set; }
    }
}
