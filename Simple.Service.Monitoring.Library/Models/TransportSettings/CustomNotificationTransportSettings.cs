namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class CustomNotificationTransportSettings : AlertTransportSettings
    {
        public string BaseEndpoint { get; set; }
        public string ApiKey { get; set; }
        public string ProjectName { get; set; }
        public string Environment { get; set; }
    }
}
