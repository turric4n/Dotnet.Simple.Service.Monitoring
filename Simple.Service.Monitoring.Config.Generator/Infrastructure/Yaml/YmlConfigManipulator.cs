using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace Simple.Service.Monitoring.Config.Generator.Infrastructure.Yaml
{
    public class YmlConfigManipulator<T> : IConfigManipulator<T>
    {
        private readonly ILogger<YmlConfigManipulator<T>> _logger;
        private readonly IDeserializer _deserializer;
        private readonly ISerializer _serializer;
        public YmlConfigManipulator(ILogger<YmlConfigManipulator<T>> logger)
        {
            _logger = logger;

            _deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .Build();

            _serializer = new Serializer();
        }

        public T Deserialize(Stream stream)
        {
            using TextReader tr = new StreamReader(stream);
            
            return _deserializer.Deserialize<T>(tr);
            
        }

        public string Serialize(T model)
        {
            return _serializer.Serialize(model);
        }
    }
}
