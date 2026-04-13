using System;

namespace Kythr.Library.Monitoring.Exceptions
{
    public class InvalidConnectionStringException : Exception
    {
        public InvalidConnectionStringException(string message) : base($"Invalid connection string has been passed")
        {
        }
    }
}
