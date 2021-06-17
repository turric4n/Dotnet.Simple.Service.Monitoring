using System;

namespace Simple.Service.Monitoring.Library.Monitoring.Exceptions
{
    public class InvalidConnectionStringException : Exception
    {
        public InvalidConnectionStringException(string message) : base($"Invalid connection string has been passed")
        {
        }
    }
}
