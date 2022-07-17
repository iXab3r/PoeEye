using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PoeShared.Modularity;

public sealed class JsonUnixTimestampConverter : DateTimeConverterBase
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        writer.WriteRawValue(DateTimeUtils.ConvertToUnixTimestamp((DateTime)value).ToString(CultureInfo.InvariantCulture));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return reader.Value == null ? DateTime.UnixEpoch : DateTimeUtils.ConvertFromUnixTimestamp(Convert.ToDouble(reader.Value));
    }
}