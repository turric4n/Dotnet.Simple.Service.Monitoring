using Simple.Service.Monitoring.Library.Models.TransportSettings;

namespace Simple.Service.Monitoring.Library.Models.TransportSettings
{
    public class SignalRTransportSettings : AlertTransportSettings
    {
        /// <summary>
        /// Gets or sets the name of the SignalR hub method to call.
        /// Default is "ReceiveHealthAlert"
        /// </summary>
        public string HubMethod { get; set; } = "ReceiveHealthAlert";

        public string HubUrl { get; set; }
    }
}
