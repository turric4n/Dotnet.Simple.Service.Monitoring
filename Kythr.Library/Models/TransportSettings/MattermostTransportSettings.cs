namespace Kythr.Library.Models.TransportSettings
{
    public class MattermostTransportSettings : AlertTransportSettings
    {
        public string WebhookUrl { get; set; }
        public string Channel { get; set; }
        public string Username { get; set; }
        public string IconUrl { get; set; }
    }
}
