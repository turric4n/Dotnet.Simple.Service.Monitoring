namespace Kythr.Library.Models.TransportSettings
{
    public class DiscordTransportSettings : AlertTransportSettings
    {
        public string WebhookUrl { get; set; }
        public string Username { get; set; }
        public string AvatarUrl { get; set; }
    }
}
