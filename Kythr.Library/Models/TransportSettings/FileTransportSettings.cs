namespace Kythr.Library.Models.TransportSettings
{
    public class FileTransportSettings : AlertTransportSettings
    {
        public string FilePath { get; set; }
        public long MaxFileSizeBytes { get; set; } = 10485760; // 10MB
        public string RollingPolicy { get; set; } = "daily";
    }
}
