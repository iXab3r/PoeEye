using Newtonsoft.Json;

namespace PoeShared.Modularity;

internal sealed class BinaryResourceReferenceConverter : JsonConverter
{
    private static readonly Lazy<BinaryResourceReferenceConverter> InstanceSupplier = new();
    
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
            {
                string SHA1 = null;
                string uri = null;
                byte[] data = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var propertyName = reader.Value.ToString();
                        reader.Read();

                        switch (propertyName)
                        {
                            case nameof(BinaryResourceReference.Uri):
                            {
                                uri = reader.Value.ToString();
                                break;
                            }
                            case nameof(BinaryResourceReference.SHA1):
                            {
                                SHA1 = reader.Value.ToString();
                                break;
                            }
                            case nameof(BinaryResourceReference.Data):
                            {
                                var base64Data = reader.Value.ToString();
                                data = string.IsNullOrEmpty(base64Data)
                                    ? Array.Empty<byte>()
                                    : Convert.FromBase64String(base64Data);
                                break;
                            }
                        }
                    }
                    else if (reader.TokenType == JsonToken.EndObject)
                    {
                        break;
                    }
                }

                return new BinaryResourceReference
                {
                    Uri = uri,
                    Data = data,
                    SHA1 = SHA1
                };
            }
            case JsonToken.String:
            {
                var base64Data = reader.Value.ToString();
                var data = string.IsNullOrEmpty(base64Data)
                    ? Array.Empty<byte>()
                    : Convert.FromBase64String(base64Data);

                return new BinaryResourceReference
                {
                    Uri = null,
                    Data = data
                };
            }
            default:
                throw new JsonSerializationException("Unexpected token type: " + reader.TokenType);
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var binaryData = (BinaryResourceReference) value;

        var isEmpty = string.IsNullOrEmpty(binaryData.Uri) && 
                      string.IsNullOrEmpty(binaryData.SHA1);

        if (isEmpty)
        {
            if (binaryData.Data != null)
            {
                var base64Data = Convert.ToBase64String(binaryData.Data);
                writer.WriteValue(base64Data);
            }
            else
            {
                throw new ArgumentException($"Data must be set in object {binaryData}");
            }
        }
        else
        {
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(binaryData.Uri))
            {
                writer.WritePropertyName(nameof(binaryData.Uri));
                writer.WriteValue(binaryData.Uri);
            }
            
            if (!string.IsNullOrEmpty(binaryData.SHA1))
            {
                writer.WritePropertyName(nameof(binaryData.SHA1));
                writer.WriteValue(binaryData.SHA1);
            }
            
            if (binaryData.Data != null)
            {
                writer.WritePropertyName(nameof(binaryData.Data));
                var base64Data = Convert.ToBase64String(binaryData.Data);
                writer.WriteValue(base64Data);
            }
            
            writer.WriteEndObject();
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(BinaryResourceReference);
    }
}