using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Simple.Service.Monitoring.Library.Application.Services.Culture
{
    public class CultureService : ICultureService
    {
        public void SetCulture(string cultureName)
        {
            CultureInfo.CurrentCulture = new CultureInfo(cultureName, false);
        }
    }
}
