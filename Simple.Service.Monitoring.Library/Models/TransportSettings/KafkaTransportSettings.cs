namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class KafkaTransportSettings : AlertTransportSettings
    {
        public string BootstrapServers { get; set; }
        public string Topic { get; set; } = "health-checks";
        public string ClientId { get; set; } = "health-check-publisher";
    }
}
