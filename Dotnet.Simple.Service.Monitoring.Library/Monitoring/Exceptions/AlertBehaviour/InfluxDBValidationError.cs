using System;
using System.Collections.Generic;
using System.Text;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class InfluxDBValidationError : Exception
    {
        public InfluxDBValidationError(string message) : base(message)
        {
        }
    }
}
