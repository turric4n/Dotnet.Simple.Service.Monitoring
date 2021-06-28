using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Models
{
    public enum AlertTransportMethod
    {
        Dummy,
        Email,
        Telegram,
        Influx,
        Slack,
        CustomApi
    }
}
