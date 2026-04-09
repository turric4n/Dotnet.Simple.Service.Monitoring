namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class ElasticsearchTransportSettings : AlertTransportSettings
    {
        public string[] Nodes { get; set; }
        public string IndexPrefix { get; set; } = "health-checks";
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
