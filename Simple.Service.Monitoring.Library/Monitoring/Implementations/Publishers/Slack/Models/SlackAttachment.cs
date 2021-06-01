using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Models
{
    public class SlackAttachment
    {
        [JsonProperty("fallback")]
        public string Fallback { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("image_url")]
        public string Image_Url { get; set; }
        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
