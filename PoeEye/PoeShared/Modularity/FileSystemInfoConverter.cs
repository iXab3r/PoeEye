using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PoeShared.Modularity;

public sealed class FileSystemInfoConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(FileSystemInfo).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var jObject = JObject.Load(reader);
        return Activator.CreateInstance(objectType, jObject.Value<string>());
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var info = value as FileSystemInfo;
        var obj = info?.FullName;
        var token = JToken.FromObject(obj);
        token.WriteTo(writer);
    }
}