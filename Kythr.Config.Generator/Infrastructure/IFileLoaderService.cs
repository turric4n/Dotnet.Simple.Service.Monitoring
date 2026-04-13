using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kythr.Config.Generator.Infrastructure
{
    public interface IFileLoaderService
    {
        Stream LoadFile(string path);
    }
}
