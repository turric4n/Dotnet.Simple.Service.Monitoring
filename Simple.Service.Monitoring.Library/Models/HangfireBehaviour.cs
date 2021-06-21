using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Models
{
    public class HangfireBehaviour  
    {
        public int MaximumJobsFailed { get; set; }
        public int MinimumAvailableServers { get; set; }
    }
}
