namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class OpsgenieTransportSettings : AlertTransportSettings
    {
        public string ApiKey { get; set; }
        public string Priority { get; set; } = "P3";
    }
}
