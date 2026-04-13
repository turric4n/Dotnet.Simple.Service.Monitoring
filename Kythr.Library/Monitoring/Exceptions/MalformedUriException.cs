using System;

namespace Kythr.Library.Monitoring.Exceptions
{
    public class MalformedUriException : Exception
    {
        public MalformedUriException(string message) : base($"Malformed URL has been passed")
        {
        }
    }
}
