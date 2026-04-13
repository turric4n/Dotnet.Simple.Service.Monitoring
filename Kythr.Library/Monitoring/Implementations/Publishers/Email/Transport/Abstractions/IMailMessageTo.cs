using System.Collections.Generic;

namespace Kythr.Library.Monitoring.Implementations.Publishers.Email.Transport.Abstractions
{
    public interface IMailMessageTo
    {
        List<string> To { get; set; }
    }
}