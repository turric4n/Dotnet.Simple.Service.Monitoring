using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Models
{
    public class SlackMessage
    {
        [JsonProperty("channel")]
        public string Channel { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("as_user")]
        public bool As_user { get; set; }
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("attachments")]
        public SlackAttachment[] Attachments { get; set; }
    }
}
