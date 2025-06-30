using Simple.Service.Monitoring.UI.Repositories;

namespace Simple.Service.Monitoring.UI.Settings
{
    public class MonitoringUiSettings
    {
        public string UiHeaderName { get; set; }
        public string UiHeaderDescription { get; set; }
        public string UiHeaderVersion { get; set; }
        public string UiHeaderLogoUrl { get; set; }
        public DataRepositoryType DataRepositoryType { get; set; } = DataRepositoryType.InMemory;
    }
}
