using Simple.Service.Monitoring.Library.Options;

namespace Simple.Service.Monitoring.Config.Generator.Config
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
