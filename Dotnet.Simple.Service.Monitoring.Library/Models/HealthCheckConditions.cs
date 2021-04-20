namespace Dotnet.Simple.Service.Monitoring.Library.Models
{
    public class HealthCheckConditions
    {
        // HTTP
        public string HttpValidStatusCodes { get; set; }
        public int HttpResponseTimes { get; set; }
        // TCP, DNS, ICMP
        public bool ServiceReach { get; set; }
        public bool ServiceConnectionEstablished { get; set; }
    }
}
