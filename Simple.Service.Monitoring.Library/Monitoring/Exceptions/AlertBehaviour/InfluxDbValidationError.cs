using System;

namespace Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class RedisValidationError : Exception
    {
        public RedisValidationError(string message) : base(message)
        {
        }
    }
}
