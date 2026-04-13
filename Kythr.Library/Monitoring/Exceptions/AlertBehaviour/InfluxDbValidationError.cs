using System;

namespace Kythr.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class RedisValidationError : Exception
    {
        public RedisValidationError(string message) : base(message)
        {
        }
    }
}
