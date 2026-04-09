namespace Simple.Service.Monitoring.Library.Models
{
    public class DockerBehaviour : ConnectionBehaviour
    {
        public string ContainerNameOrId { get; set; }
        public string DockerEndpoint { get; set; } = "npipe://./pipe/docker_engine";
    }
}
