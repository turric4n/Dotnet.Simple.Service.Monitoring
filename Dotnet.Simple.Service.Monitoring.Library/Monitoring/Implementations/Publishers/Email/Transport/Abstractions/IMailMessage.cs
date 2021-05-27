using System.Collections.Generic;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions
{
    public interface IMailMessage
    {
        string Subject { get; set; }
        string DisplayName { get; }
        bool IsBodyHtml { get; set; }
        string Body { get; set; }
        string FromEmail { get; }
        string ToAsString { get; }        
        IEnumerable<string> To { get; }
        void Add(string to);
    }
}