using System;
using System.Collections.Generic;
using System.Text;

namespace Simple.Service.Monitoring.Library.Monitoring.Implementations.Publishers.Email.Templates.Abstractions
{
    public interface ITemplateProvider
    {
        string GetTemplate(string Name);
    }
}
