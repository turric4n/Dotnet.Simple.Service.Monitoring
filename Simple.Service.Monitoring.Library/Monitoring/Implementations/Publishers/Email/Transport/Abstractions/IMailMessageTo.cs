using System.Collections.Generic;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions
{
    public interface IMailMessageTo
    {
        List<string> To { get; set; }
    }
}