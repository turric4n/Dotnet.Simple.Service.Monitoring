using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Simple.Service.Monitoring.Library.Models.TransportSettings;
using Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Implementations
{
    public class SmtpMailMessage : MailMessage, IMailMessage
    {
        public string FromEmail => _mailingSettings.From;
        public new string Subject
        {
            get => base.Subject;
            set => base.Subject = value;
        }

        public string DisplayName
        {
            get => base.From.DisplayName;
        }

        public new bool IsBodyHtml
        {
            get => base.IsBodyHtml;
            set => base.IsBodyHtml = value;
        }

        IEnumerable<string> IMailMessage.To => base.To
            .Select(to => to.Address)
            .ToList();

        string IMailMessage.ToAsString => base.To.ToString();

        private readonly EmailTransportSettings _mailingSettings;
        public SmtpMailMessage(EmailTransportSettings mailingSettings)
        {
            _mailingSettings = mailingSettings;

            this.From = new MailAddress(this.FromEmail, _mailingSettings.DisplayName);
        }

        public void Add(string to)
        {
            this.To.Add(to);
        }
    }
}