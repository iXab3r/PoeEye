using System.Text.Json;
using System.Text.Json.Serialization;

namespace PoeShared.Scaffolding;

internal sealed class PercentageSystemTextJsonConverter : JsonConverter<Percentage>
{
    public override Percentage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // number: 42.5
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetSingle(out var f))
            {
                return new Percentage(f);
            }

            if (reader.TryGetDouble(out var d))
            {
                return new Percentage((float) d);
            }

            throw new JsonException("Invalid numeric value for Percentage.");
        }

        // string: "42.5" or "42.5%"
        if (reader.TokenType == JsonTokenType.String)
        {
            return new Percentage(ParseFloat(reader.GetString()));
        }

        // object: { "Value": 42.5 } or { "value": "42.5%" }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Expected object for Percentage.");
            }

            if (!TryGetCaseInsensitiveProperty(root, "Value", out var valueProp))
            {
                throw new JsonException("Object missing 'Value' property for Percentage.");
            }

            switch (valueProp.ValueKind)
            {
                case JsonValueKind.Number:
                    if (valueProp.TryGetSingle(out var f))
                    {
                        return new Percentage(f);
                    }

                    if (valueProp.TryGetDouble(out var d))
                    {
                        return new Percentage((float) d);
                    }

                    break;

                case JsonValueKind.String:
                    return new Percentage(ParseFloat(valueProp.GetString()));
            }

            throw new JsonException("Unsupported 'Value' property type for Percentage.");
        }

        throw new JsonException("Expected number, string, or object for Percentage.");

        // --- local helpers ---
        static float ParseFloat(string? s)
        {
            if (s is null)
            {
                throw new JsonException("Null string for Percentage.");
            }

            s = s.Trim().TrimEnd('%');
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
            {
                return f;
            }

            throw new JsonException($"Invalid string for Percentage: '{s}'.");
        }

        static bool TryGetCaseInsensitiveProperty(JsonElement obj, string name, out JsonElement value)
        {
            foreach (var p in obj.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }

    public override void Write(Utf8JsonWriter writer, Percentage value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Value);
    }
}