using Kythr.Library.Models.TransportSettings;
using Kythr.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions;
using System.Net;
using System.Net.Mail;

namespace Kythr.Library.Monitoring.Implementations.Publishers.Email.Transport.Implementations
{
    public class SmtpMailSender : IMailSenderClient
    {
        private readonly EmailTransportSettings _settings;

        public SmtpMailSender(EmailTransportSettings settings)
        {
            _settings = settings;
        }

        public void SendMessage(IMailMessage msg)
        {

            using var smtpclient = _settings.SmtpPort > 0
                ? new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
                : new SmtpClient(_settings.SmtpHost);

            if (_settings.Authentication && !string.IsNullOrEmpty(_settings.Username))
            {
                smtpclient.Credentials = new NetworkCredential(_settings.Username, _settings.Password);
                smtpclient.EnableSsl = true;
            }

            smtpclient.Send((MailMessage)msg);
        }
    }
}
