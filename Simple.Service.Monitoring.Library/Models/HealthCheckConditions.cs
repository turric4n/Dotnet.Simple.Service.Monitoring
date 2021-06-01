namespace Simple.Service.Monitoring.Library.Models
{
    public class HealthCheckConditions
    {
        // HTTP
        public HttpBehaviour HttpBehaviour { get; set; }

        // TCP, DNS, ICMP
        public bool ServiceReach { get; set; }
        public bool ServiceConnectionEstablished { get; set; }
    }
}
