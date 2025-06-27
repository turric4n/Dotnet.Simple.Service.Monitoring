using System.Collections.Generic;

namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class WebhookTransportSettings : AlertTransportSettings
    {
        public string WebhookUrl { get; set; }
        public HttpBehaviour HttpBehaviour { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public WebhookTransportSettings()
        {
            HttpBehaviour = new HttpBehaviour();
        }
    }
}
