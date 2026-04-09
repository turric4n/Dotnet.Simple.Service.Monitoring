namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class CloudWatchTransportSettings : AlertTransportSettings
    {
        public string Region { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Namespace { get; set; } = "HealthChecks";
    }
}
