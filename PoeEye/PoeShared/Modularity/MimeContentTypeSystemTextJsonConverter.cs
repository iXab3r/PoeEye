namespace PoeShared.Modularity;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class MimeContentTypeSystemTextJsonConverter : JsonConverter<MimeContentType>
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(MimeContentType);
    }

    public override MimeContentType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // string: "text/html"
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonException("MimeContentType cannot be null or empty.");
            return new MimeContentType(s);
        }

        // object: { "MediaType": "text/html" } or { "Value": "text/html" }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException("Expected object for MimeContentType.");

            if (!TryGetCaseInsensitiveProperty(root, "MediaType", out var prop) &&
                !TryGetCaseInsensitiveProperty(root, "Value", out prop))
            {
                throw new JsonException("Object missing 'MediaType' (or 'Value') property for MimeContentType.");
            }

            if (prop.ValueKind != JsonValueKind.String)
                throw new JsonException("MediaType/Value must be a string.");

            var s = prop.GetString();
            if (string.IsNullOrWhiteSpace(s))
                throw new JsonException("MimeContentType cannot be null or empty.");

            return new MimeContentType(s);
        }

        throw new JsonException("Expected string or object for MimeContentType.");

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

    public override void Write(Utf8JsonWriter writer, MimeContentType value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.MediaType);
}