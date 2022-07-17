using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.Modularity;

public sealed class JsonTimeSpanToSecondsConverter : DateTimeConverterBase
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteRawValue(((TimeSpan)value).TotalSeconds.ToString("F0", CultureInfo.InvariantCulture));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.Value == null)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(Convert.ToDouble(reader.Value));
    }
}