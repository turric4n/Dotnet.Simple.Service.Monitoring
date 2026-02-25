using System.Collections.Generic;

namespace Simple.Service.Monitoring.Library.Models
{
    public enum HttpVerb
    {
        Get,
        Post,
        Put,
        Delete
    }
    public class HttpBehaviour : ConnectionBehaviour
    {
        public HttpBehaviour()
        {
            CustomHttpHeaders = new Dictionary<string, string>();
        }

        public int HttpExpectedCode { get; set; } = 200;
        public int HttpTimeoutMs { get; set; } = 5000;
        public HttpVerb HttpVerb { get; set; }
        public Dictionary<string, string> CustomHttpHeaders { get; set; }
    }
}
