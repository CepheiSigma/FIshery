using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Fishery.Core.Serialize
{
    public class Serializer
    {
        private JsonSerializer _jsonSerializer;

        public Serializer()
        {
            _jsonSerializer = new JsonSerializer();
            _jsonSerializer.Error += (sender, e) => e.ErrorContext.Handled = true;
        }

        public string SerializeToString(object data)
        {
            StringWriter stringWriter = new StringWriter();
            _jsonSerializer.Serialize(stringWriter, data);
            return stringWriter.GetStringBuilder().ToString();
        }

        public void SerializeToFile(object data, string filePath)
        {
            File.WriteAllText(filePath, SerializeToString(data));
        }

        public T DeSerializeFromString<T>(string content)
        {
            return _jsonSerializer.Deserialize<T>(new JsonTextReader(new StringReader(content)));
        }

        public T DeSerializeFromFile<T>(string filePath)
        {
            if (!File.Exists(filePath))
                return default(T);
            return
                DeSerializeFromString<T>(
                    File.ReadAllText(filePath));
        }

        public T ConvertBySerialize<T>(object o)
        {
            return _jsonSerializer.Deserialize<T>(new JsonTextReader(new StringReader(SerializeToString(o))));
        }
    }
}