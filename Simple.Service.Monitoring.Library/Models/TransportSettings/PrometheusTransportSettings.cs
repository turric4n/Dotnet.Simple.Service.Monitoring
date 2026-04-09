namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class PrometheusTransportSettings : AlertTransportSettings
    {
        public string PushgatewayUrl { get; set; }
        public string JobName { get; set; } = "health_checks";
    }
}
