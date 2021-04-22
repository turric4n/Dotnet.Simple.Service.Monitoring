using System;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions
{
    public class MalformedUriException : Exception
    {
        public MalformedUriException(string message) : base($"Malformed URL has been passed")
        {
        }
    }
}
