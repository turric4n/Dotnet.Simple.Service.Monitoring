namespace Kythr.Library.Models.TransportSettings
{
    public class AppInsightsTransportSettings : AlertTransportSettings
    {
        public string ConnectionString { get; set; }
        public string InstrumentationKey { get; set; }
    }
}
