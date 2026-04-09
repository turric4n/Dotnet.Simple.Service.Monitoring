namespace Simple.Service.Monitoring.Library.Models
{
    public class GrpcBehaviour : ConnectionBehaviour
    {
        public bool UseHealthCheckProtocol { get; set; } = true;
    }
}
