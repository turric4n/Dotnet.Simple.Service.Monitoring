using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Implementations
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

            smtpclient.Send((MailMessage)msg);
        }
    }
}
