using System;
using System.Collections.Generic;
using System.Text;

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
        public int HttpExpectedCode { get; set; }
        public int HttpTimeoutMs { get; set; } = 5000;
        public HttpVerb HttpVerb { get; set; }
    }
}
