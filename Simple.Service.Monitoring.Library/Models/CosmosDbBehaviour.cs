namespace Simple.Service.Monitoring.Library.Models
{
    public class CosmosDbBehaviour : ConnectionBehaviour
    {
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; }
    }
}
