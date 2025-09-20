using System.Net.Mime;
using Newtonsoft.Json;

namespace PoeShared.Modularity;

public sealed class BinaryResourceRefConverter : JsonConverter
{
    private static readonly Lazy<BinaryResourceRefConverter> InstanceSupplier = new();
    
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        switch (reader.TokenType)
        {
            case JsonToken.StartObject:
            {
                string hash = null;
                string uri = null;
                string fileName = null;
                string cipherSuite = null;
                string cipherKeySalt = null;
                byte[] data = null;
                int? contentLength = null;
                DateTimeOffset? lastModified = null;
                MimeContentType? contentType = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        var propertyName = reader.Value.ToString();
                        switch (propertyName)
                        {
                            case nameof(BinaryResourceRef.Uri):
                            {
                                uri = reader.ReadAsString();
                                break;
                            }
                            case nameof(BinaryResourceRef.CipherSuite):
                            {
                                cipherSuite = reader.ReadAsString();
                                break;
                            }  
                            case nameof(BinaryResourceRef.CipherKeySalt):
                            {
                                cipherKeySalt = reader.ReadAsString();
                                break;
                            } 
                            case nameof(BinaryResourceRef.Hash):
                            {
                                hash = reader.ReadAsString();
                                break;
                            }
                            case nameof(BinaryResourceRef.FileName):
                            {
                                fileName = reader.ReadAsString();
                                break;
                            }
                            case nameof(BinaryResourceRef.ContentLength):
                            {
                                contentLength = reader.ReadAsInt32();
                                break;
                            }
                            case nameof(BinaryResourceRef.LastModified):
                            {
                                lastModified = reader.ReadAsDateTimeOffset();
                                break;
                            }
                            case nameof(BinaryResourceRef.ContentType):
                            {
                                // Move to the value token
                                if (!reader.Read())
                                {
                                    throw new JsonSerializationException("Unexpected end while reading ContentType.");
                                }

                                if (reader.TokenType == JsonToken.Null)
                                {
                                    contentType = null;
                                }
                                else
                                {
                                    // Delegate to serializer so it can invoke MimeContentTypeNewtonsoftJsonConverter
                                    var parsed = serializer.Deserialize<MimeContentType>(reader);
                                    contentType = parsed;
                                }
                                break;
                            }
                            case nameof(BinaryResourceRef.Data):
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

                return new BinaryResourceRef
                {
                    Uri = uri,
                    Data = data,
                    CipherSuite = cipherSuite,
                    CipherKeySalt = cipherKeySalt,
                    Hash = hash,
                    ContentType = contentType,
                    ContentLength = contentLength,
                    FileName = fileName,
                    LastModified = lastModified,
                };
            }
            case JsonToken.String:
            {
                //non-encrypted raw data array
                var base64Data = reader.Value.ToString();
                var data = string.IsNullOrEmpty(base64Data)
                    ? null
                    : Convert.FromBase64String(base64Data);

                return new BinaryResourceRef
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
        var binaryData = (BinaryResourceRef) value;
        if (binaryData.HasMetadata)
        {
            writer.WriteStartObject();

            if (!string.IsNullOrEmpty(binaryData.Uri))
            {
                writer.WritePropertyName(nameof(binaryData.Uri));
                writer.WriteValue(binaryData.Uri);
            }
            
            if (!string.IsNullOrEmpty(binaryData.CipherSuite))
            {
                writer.WritePropertyName(nameof(binaryData.CipherSuite));
                writer.WriteValue(binaryData.CipherSuite);
            }
            
            if (!string.IsNullOrEmpty(binaryData.CipherKeySalt))
            {
                writer.WritePropertyName(nameof(binaryData.CipherKeySalt));
                writer.WriteValue(binaryData.CipherKeySalt);
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

            if (!string.IsNullOrEmpty(binaryData.Hash))
            {
                writer.WritePropertyName(nameof(binaryData.Hash));
                writer.WriteValue(binaryData.Hash);
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
            var base64Data = Convert.ToBase64String(binaryData.Data ?? Array.Empty<byte>());
            writer.WriteValue(base64Data);
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(BinaryResourceRef);
    }
}