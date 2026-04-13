namespace Kythr.Library.Models.TransportSettings
{
    public class DatadogTransportSettings : AlertTransportSettings
    {
        public string ApiKey { get; set; }
        public string ApplicationKey { get; set; }
        public string Site { get; set; } = "datadoghq.com";
    }
}
