using Simple.Service.Monitoring.UI.Repositories;

namespace Simple.Service.Monitoring.UI.Options
{
    public class MonitoringUiOptions
    {
        public string CompanyName { get; set; }
        public string HeaderLogoUrl { get; set; }
        public string HeaderDescription { get; set; }
        public string Version { get; set; }
        public DataRepositoryType DataRepositoryType { get; set; } = DataRepositoryType.LiteDb;
    }
}
