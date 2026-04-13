using Kythr.Library.Options;

namespace Kythr.Config.Generator.Config
{
    public class MonitoringWrapper
    {
        public MonitoringWrapper(MonitorOptions monitoring)
        {
            Monitoring = monitoring;
        }

        public MonitorOptions Monitoring { get; set; }
    }
}
