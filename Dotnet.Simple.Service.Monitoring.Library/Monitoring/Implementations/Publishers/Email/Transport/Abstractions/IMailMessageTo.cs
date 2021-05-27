using System.Collections.Generic;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions
{
    public interface IMailMessageTo
    {
        List<string> To { get; set; }
    }
}