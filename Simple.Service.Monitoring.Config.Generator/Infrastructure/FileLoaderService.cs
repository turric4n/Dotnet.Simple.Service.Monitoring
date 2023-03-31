namespace Simple.Service.Monitoring.Config.Generator.Infrastructure
{
    public class FileLoaderService : IFileLoaderService
    {
        public Stream LoadFile(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new FileNotFoundException($"File not found : {path}");
            }

            var stream = new FileStream(path, FileMode.Open);

            return stream;
        }
    }
}
