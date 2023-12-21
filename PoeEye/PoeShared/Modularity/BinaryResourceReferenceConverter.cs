using System.Net.Mime;
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
                string sha1 = null;
                string uri = null;
                string fileName = null;
                byte[] data = null;
                int? contentLength = null;
                DateTimeOffset? lastModified = null;
                ContentType contentType = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var propertyName = reader.Value.ToString();
                        switch (propertyName)
                        {
                            case nameof(BinaryResourceReference.Uri):
                            {
                                uri = reader.ReadAsString();
                                break;
                            }
                            case nameof(BinaryResourceReference.SHA1):
                            {
                                sha1 = reader.ReadAsString();
                                break;
                            }
                            case nameof(BinaryResourceReference.FileName):
                            {
                                fileName = reader.ReadAsString();
                                break;
                            }
                            case nameof(BinaryResourceReference.ContentLength):
                            {
                                contentLength = reader.ReadAsInt32();
                                break;
                            }
                            case nameof(BinaryResourceReference.LastModified):
                            {
                                lastModified = reader.ReadAsDateTimeOffset();
                                break;
                            }
                            case nameof(BinaryResourceReference.ContentType):
                            {
                                contentType = new ContentType(reader.ReadAsString());
                                break;
                            }
                            case nameof(BinaryResourceReference.Data):
                            {
                                data = reader.ReadAsBytes();
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
                    SHA1 = sha1,
                    ContentType = contentType,
                    ContentLength = contentLength,
                    FileName = fileName,
                    LastModified = lastModified
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
        if (binaryData.HasMetadata)
        {
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(binaryData.Uri))
            {
                writer.WritePropertyName(nameof(binaryData.Uri));
                writer.WriteValue(binaryData.Uri);
            }

            if (!string.IsNullOrEmpty(binaryData.FileName))
            {
                writer.WritePropertyName(nameof(binaryData.FileName));
                writer.WriteValue(binaryData.FileName);
            }

            if (binaryData.ContentType != null)
            {
                writer.WritePropertyName(nameof(binaryData.ContentType));
                writer.WriteValue(binaryData.ContentType.ToString());
            }

            if (binaryData.ContentLength != null)
            {
                writer.WritePropertyName(nameof(binaryData.ContentLength));
                writer.WriteValue(binaryData.ContentLength);
            }

            if (binaryData.LastModified != null)
            {
                writer.WritePropertyName(nameof(binaryData.LastModified));
                writer.WriteValue(binaryData.LastModified);
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
        else
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
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(BinaryResourceReference);
    }
}