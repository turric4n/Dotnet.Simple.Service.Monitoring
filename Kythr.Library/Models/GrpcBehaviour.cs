namespace Kythr.Library.Models
{
    public class GrpcBehaviour : ConnectionBehaviour
    {
        public bool UseHealthCheckProtocol { get; set; } = true;
    }
}
