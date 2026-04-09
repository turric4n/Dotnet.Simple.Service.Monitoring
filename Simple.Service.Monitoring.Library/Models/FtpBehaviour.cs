namespace Simple.Service.Monitoring.Library.Models
{
    public class FtpBehaviour : ConnectionBehaviour
    {
        public bool UseSftp { get; set; }
        public int Port { get; set; } = 21;
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
