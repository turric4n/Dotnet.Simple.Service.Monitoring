namespace Simple.Service.Monitoring.Library.Models
{
    public class AzureServiceBusBehaviour : ConnectionBehaviour
    {
        public string QueueOrTopicName { get; set; }
    }
}
