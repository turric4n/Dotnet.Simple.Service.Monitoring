namespace Kythr.Library.Models
{
    public class SmtpBehaviour : ConnectionBehaviour
    {
        public int Port { get; set; } = 25;
        public bool UseTls { get; set; }
    }
}
