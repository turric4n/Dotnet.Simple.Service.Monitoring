using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Slack.Models
{
    public class SlackMessageResponse
    {
        [JsonProperty("ok")]
        public bool Ok { get; set; }
        [JsonProperty("error")]
        public string Error { get; set; }
        [JsonProperty("channel")]
        public string Channel { get; set; }
        [JsonProperty("ts")]
        public string Ts { get; set; }
    }
}
