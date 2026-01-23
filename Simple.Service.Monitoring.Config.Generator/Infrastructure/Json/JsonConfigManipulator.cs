using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Simple.Service.Monitoring.Config.Generator.Infrastructure.Json
{
    public class JsonConfigManipulator<T> : IConfigManipulator<T>
    {
        private readonly ILogger<JsonConfigManipulator<T>> _logger;

        public JsonConfigManipulator(ILogger<JsonConfigManipulator<T>> logger)
        {
            _logger = logger;
        }

        public T? Deserialize(Stream stream)
        {
            using TextReader tr = new StreamReader(stream);
            var text = tr.ReadToEnd();

            return JsonConvert.DeserializeObject<T>(text);
        }

        public string Serialize(T model)
        {
            return JsonConvert.SerializeObject(model);
        }
    }
}
