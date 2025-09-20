namespace PoeShared.Modularity;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class MimeContentTypeNewtonsoftJsonConverter : JsonConverter<MimeContentType>
{
    public override MimeContentType ReadJson(JsonReader reader, Type objectType, MimeContentType existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            var s = (string?)reader.Value;
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonSerializationException("MimeContentType cannot be null or empty.");
            return new MimeContentType(s);
        }

        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);

            var prop = obj.Property("MediaType", StringComparison.OrdinalIgnoreCase)
                       ?? obj.Property("Value", StringComparison.OrdinalIgnoreCase)
                       ?? throw new JsonSerializationException("Object missing 'MediaType' (or 'Value') property for MimeContentType.");

            if (prop.Value.Type != JTokenType.String)
                throw new JsonSerializationException("MediaType/Value must be a string.");

            var s = prop.Value.Value<string>();
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonSerializationException("MimeContentType cannot be null or empty.");

            return new MimeContentType(s);
        }

        throw new JsonSerializationException("Expected string or object for MimeContentType.");
    }

    public override void WriteJson(JsonWriter writer, MimeContentType value, JsonSerializer serializer)
        => writer.WriteValue(value.MediaType);
}