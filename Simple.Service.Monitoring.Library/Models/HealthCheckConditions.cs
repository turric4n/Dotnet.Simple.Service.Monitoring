namespace Simple.Service.Monitoring.Library.Models
{
    public class HealthCheckConditions
    {
        // HTTP
        public HealthCheckConditions()
        {
            HttpBehaviour = new HttpBehaviour();
            SqlBehaviour = new SqlBehaviour();
            HangfireBehaviour = new HangfireBehaviour();
            RedisBehaviour = new RedisBehaviour();
        }

        public HttpBehaviour HttpBehaviour { get; set; }

        public SqlBehaviour SqlBehaviour { get; set; }

        public HangfireBehaviour HangfireBehaviour { get; set; }

        public RedisBehaviour RedisBehaviour { get; set; }

        // TCP, DNS, ICMP
        public bool ServiceReach { get; set; }
        public bool ServiceConnectionEstablished { get; set; }
    }
}
