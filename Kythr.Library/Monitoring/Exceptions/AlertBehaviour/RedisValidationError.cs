using System;
using System.Collections.Generic;
using System.Text;

namespace Kythr.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class InfluxDbValidationError : Exception
    {
        public InfluxDbValidationError(string message) : base(message)
        {
        }
    }
}
