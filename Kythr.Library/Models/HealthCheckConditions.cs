namespace Kythr.Library.Models
{
    public class HealthCheckConditions
    {
        public HealthCheckConditions()
        {
            HttpBehaviour = new HttpBehaviour();
            SqlBehaviour = new SqlBehaviour();
            HangfireBehaviour = new HangfireBehaviour();
            RedisBehaviour = new RedisBehaviour();
            MongoDbBehaviour = new MongoDbBehaviour();
            CosmosDbBehaviour = new CosmosDbBehaviour();
            KafkaBehaviour = new KafkaBehaviour();
            GrpcBehaviour = new GrpcBehaviour();
            TcpBehaviour = new TcpBehaviour();
            DnsBehaviour = new DnsBehaviour();
            SslCertificateBehaviour = new SslCertificateBehaviour();
            FtpBehaviour = new FtpBehaviour();
            SmtpBehaviour = new SmtpBehaviour();
            AzureServiceBusBehaviour = new AzureServiceBusBehaviour();
            MemcachedBehaviour = new MemcachedBehaviour();
            DockerBehaviour = new DockerBehaviour();
            AwsSqsBehaviour = new AwsSqsBehaviour();
            PingBehaviour = new PingBehaviour();
        }

        public HttpBehaviour HttpBehaviour { get; set; }
        public SqlBehaviour SqlBehaviour { get; set; }
        public HangfireBehaviour HangfireBehaviour { get; set; }
        public RedisBehaviour RedisBehaviour { get; set; }
        public MongoDbBehaviour MongoDbBehaviour { get; set; }
        public CosmosDbBehaviour CosmosDbBehaviour { get; set; }
        public KafkaBehaviour KafkaBehaviour { get; set; }
        public GrpcBehaviour GrpcBehaviour { get; set; }
        public TcpBehaviour TcpBehaviour { get; set; }
        public DnsBehaviour DnsBehaviour { get; set; }
        public SslCertificateBehaviour SslCertificateBehaviour { get; set; }
        public FtpBehaviour FtpBehaviour { get; set; }
        public SmtpBehaviour SmtpBehaviour { get; set; }
        public AzureServiceBusBehaviour AzureServiceBusBehaviour { get; set; }
        public MemcachedBehaviour MemcachedBehaviour { get; set; }
        public DockerBehaviour DockerBehaviour { get; set; }
        public AwsSqsBehaviour AwsSqsBehaviour { get; set; }
        public PingBehaviour PingBehaviour { get; set; }

        // TCP, DNS, ICMP
        public bool ServiceReach { get; set; }
        public bool ServiceConnectionEstablished { get; set; }
    }
}
