using System;
using System.Collections.Generic;
using System.Text;

namespace Dotnet.Simple.Service.Monitoring.Library.Models
{
    public enum HttpVerb
    {
        Get,
        Post,
        Put,
        Delete
    }
    public class HttpBehaviour
    {
        public int HttpExpectedCode { get; set; }
        public int HttpExpectedResponseTimeMs { get; set; }
        public HttpVerb HttpVerb { get; set; }
    }
}
