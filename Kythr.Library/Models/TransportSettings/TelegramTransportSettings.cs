namespace Kythr.Library.Models.TransportSettings
{
    public class TelegramTransportSettings : AlertTransportSettings
    {
        public string BotApiToken { get; set; }
        public string ChatId { get; set; }
    }
}
