namespace Simple.Service.Monitoring.Library.Models
{
    public class SslCertificateBehaviour : ConnectionBehaviour
    {
        public int WarningDaysBeforeExpiry { get; set; } = 30;
        public int Port { get; set; } = 443;
    }
}
