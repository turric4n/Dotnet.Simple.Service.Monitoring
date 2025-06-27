namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class RedisTransportSettings : AlertTransportSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 6379; // Default Redis port
        public int DatabaseNumber { get; set; } = 0;
    }
}
