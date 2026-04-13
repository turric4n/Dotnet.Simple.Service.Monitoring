namespace Kythr.Library.Models.TransportSettings
{
    public class ConsoleTransportSettings : AlertTransportSettings
    {
        public bool UseColors { get; set; } = true;
        public string OutputFormat { get; set; } = "text";
    }
}
