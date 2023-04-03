using Newtonsoft.Json;

namespace PoeShared.IO;

public sealed class OSPathConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value is OSPath osPath)
        {
            writer.WriteValue(osPath.AsWindowsPath);
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.Value is string value)
        {
            return new OSPath(value);
        }

        return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string);
    }
}