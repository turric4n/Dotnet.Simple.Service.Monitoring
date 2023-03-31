using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Service.Monitoring.Config.Generator.Infrastructure
{
    public class ExtensionValidatorService : IExtensionValidatorService
    {
        public ExtensionType GetCurrentExtension(string path)
        {
            var extension = new System.IO.FileInfo(path).Extension;

            return extension is ".yml" or ".yaml" 
                ? ExtensionType.Yaml 
                : extension is ".json" 
                    ? ExtensionType.Json 
                    : throw new Exception("Invalid extension");
        }
    }
}
