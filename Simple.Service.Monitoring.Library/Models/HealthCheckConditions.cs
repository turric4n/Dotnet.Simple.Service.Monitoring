namespace Simple.Service.Monitoring.Library.Models
{
    public class HealthCheckConditions
    {
        // HTTP
        public HttpBehaviour HttpBehaviour { get; set; }

        public MsSqlBehaviour SqlBehaviour { get; set; }

        public HangfireBehaviour HangfireBehaviour { get; set; }

        public RedisBehaviour RedisBehaviour { get; set; }

        // TCP, DNS, ICMP
        public bool ServiceReach { get; set; }
        public bool ServiceConnectionEstablished { get; set; }
    }
}
