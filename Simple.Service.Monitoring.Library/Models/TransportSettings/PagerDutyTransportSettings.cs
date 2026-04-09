namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class PagerDutyTransportSettings : AlertTransportSettings
    {
        public string RoutingKey { get; set; }
        public string Severity { get; set; } = "error";
    }
}
