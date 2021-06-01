using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class TelegramAlertingValidationError : Exception
    {
        public TelegramAlertingValidationError(string message) : base(message)
        {
        }
    }
}
