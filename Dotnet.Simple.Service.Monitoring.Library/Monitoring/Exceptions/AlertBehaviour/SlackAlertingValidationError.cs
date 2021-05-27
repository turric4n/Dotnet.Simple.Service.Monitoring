using System;
using System.Collections.Generic;
using System.Text;

namespace Dotnet.Simple.Service.Monitoring.Library.Monitoring.Exceptions.AlertBehaviour
{
    public class SlackAlertingValidationError : Exception
    {
        public SlackAlertingValidationError(string message) : base(message)
        {
        }
    }
}
