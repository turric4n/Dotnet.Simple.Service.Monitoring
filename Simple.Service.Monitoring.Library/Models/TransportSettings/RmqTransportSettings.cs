namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class RmqTransportSettings : AlertTransportSettings
    {
        public string ConnectionString { get; set; }
        public string Exchange { get; set; } = "health_checks";
        public string RoutingKey { get; set; } = "health.check.result";
        public string QueueName { get; set; }
    }
}
