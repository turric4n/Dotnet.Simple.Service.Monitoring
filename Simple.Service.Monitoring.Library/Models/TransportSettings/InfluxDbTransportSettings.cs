namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public enum InfluxDbVersion
    {
        V1,
        V2
    }

    public class InfluxDbTransportSettings : AlertTransportSettings
    {
        public string Host { get; set; }
        public string Database { get; set; }
        public bool AutoCreateDatabase { get; set; } = true;
        public string RetentionPolicy { get; set; }
        public string RetentionDuration { get; set; }
        public InfluxDbVersion Version { get; set; } = InfluxDbVersion.V1;
        public string Token { get; set; }
        public string Organization { get; set; }
    }
}
