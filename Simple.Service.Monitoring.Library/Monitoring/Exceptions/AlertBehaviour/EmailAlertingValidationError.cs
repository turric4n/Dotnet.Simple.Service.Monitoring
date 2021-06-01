using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class EmailAlertingValidationError : Exception
    {
        public EmailAlertingValidationError(string message) : base(message)
        {
        }
    }
}
