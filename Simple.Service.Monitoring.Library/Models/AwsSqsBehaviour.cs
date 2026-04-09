namespace Simple.Service.Monitoring.Library.Models
{
    public class AwsSqsBehaviour : ConnectionBehaviour
    {
        public string QueueUrl { get; set; }
        public string Region { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
    }
}
