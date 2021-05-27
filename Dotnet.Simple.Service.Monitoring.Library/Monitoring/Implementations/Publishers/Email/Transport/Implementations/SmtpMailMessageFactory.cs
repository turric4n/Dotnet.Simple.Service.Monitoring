using Dotnet.Simple.Service.Monitoring.Library.Models.TransportSettings;
using Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Implementations
{
    public class SmtpMailMessageFactory : IMailMessageFactory
    {
        private readonly EmailTransportSettings _emailTransportSettings;

        public SmtpMailMessageFactory(EmailTransportSettings emailTransportSettings)
        {
            _emailTransportSettings = emailTransportSettings;
        }

        public IMailMessage Create(string email, string subject, string body)
        {
            IMailMessage message = new SmtpMailMessage(_emailTransportSettings);

            message.Add(email);
            message.IsBodyHtml = true;
            message.Body = body;
            message.Subject = subject;

            return message;
        }
    }
}