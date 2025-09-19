using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PoeShared.Scaffolding;

internal sealed class PercentageNewtonsoftJsonConverter : JsonConverter<Percentage>
{
    public override Percentage ReadJson(JsonReader reader, Type objectType, Percentage existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
        {
            return new Percentage(Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture));
        }

        if (reader.TokenType == JsonToken.String)
        {
            return new Percentage(ParseFloat((string?) reader.Value));
        }

        if (reader.TokenType == JsonToken.StartObject)
        {
            var obj = JObject.Load(reader);
            // Case-insensitive lookup of "Value"
            var prop = obj.Property("Value", StringComparison.OrdinalIgnoreCase)
                       ?? throw new JsonSerializationException("Object missing 'Value' property for Percentage.");

            if (prop.Value.Type == JTokenType.Float || prop.Value.Type == JTokenType.Integer)
            {
                return new Percentage(prop.Value.ToObject<float>()); // uses invariant culture internally
            }

            if (prop.Value.Type == JTokenType.String)
            {
                return new Percentage(ParseFloat(prop.Value.Value<string>()));
            }

            throw new JsonSerializationException("Unsupported 'Value' property type for Percentage.");
        }

        throw new JsonSerializationException("Expected number, string, or object for Percentage.");

        // --- local helper ---
        static float ParseFloat(string? s)
        {
            if (s is null)
            {
                throw new JsonSerializationException("Null string for Percentage.");
            }

            s = s.Trim().TrimEnd('%');
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                return f;
            }

            throw new JsonSerializationException($"Invalid string for Percentage: '{s}'.");
        }
    }

    public override void WriteJson(JsonWriter writer, Percentage value, JsonSerializer serializer)
    {
        writer.WriteValue(value.Value);
    }
}