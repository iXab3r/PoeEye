using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Converters;

/// <summary>
///   Uses Windows DPAPI to protect data, key is linked to local machine
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class SafeDataConverter : JsonConverter
{
    private static readonly IFluentLog Log = typeof(SafeDataConverter).PrepareLogger();

    /// <summary>
    ///   Additional entropy makes it a bit harder to decrypt values without looking into how exactly DPAPI is called
    /// </summary>
    private static readonly byte[] AdditionalEntropy = { 2, 0, 2, 2, 0, 8, 0, 1 };

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var rawValue = value switch
        {
            SecureString secureString => secureString.ToUnsecuredString(),
            string str => str,
            _ => SerializeToString(serializer, value)
        };
        var bytesToEncode = Encoding.Default.GetBytes(rawValue);
        var encodedBytes = ProtectedData.Protect(bytesToEncode, AdditionalEntropy, DataProtectionScope.LocalMachine);
        var serializedBytes = SerializeToString(serializer, encodedBytes);
        var compressedBytes = StringUtils.CompressStringToGZip(serializedBytes, includePrefix: true);
        writer.WriteValue(compressedBytes);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.Value is not string rawValue)
        {
            return null;
        }

        try
        {
            if (StringUtils.IsGzip(rawValue))
            {
                rawValue = StringUtils.DecompressStringFromGZip(rawValue, true);

                var deserializedBytes = (byte[])DeserializeFromString(serializer, rawValue, typeof(byte[]));
                if (deserializedBytes == null || deserializedBytes.Length == 0)
                {
                    return null;
                }

                try
                {
                    var decodedBytes = ProtectedData.Unprotect(deserializedBytes, AdditionalEntropy, DataProtectionScope.LocalMachine);
                    rawValue = Encoding.Default.GetString(decodedBytes);
                }
                catch (CryptographicException e)
                {
                    Log.Warn($"Failed to decrypt array of size {deserializedBytes.Length} with entropy into type {objectType}", e);
                }
            }

            if (rawValue == null)
            {
                return null;
            }

            if (typeof(string) == objectType)
            {
                return rawValue;
            }

            if (typeof(SecureString) == objectType)
            {
                return rawValue.ToSecuredString();
            }
        
            var result = DeserializeFromString(serializer, rawValue, objectType);
            return result;
        }
        catch (Exception e)
        {
            Log.Warn($"Exception when when tried to read safe data into type {objectType}", e);
            return null;
        }
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }

    private static object DeserializeFromString(JsonSerializer serializer, string json, Type objectType)
    {
        try
        {
            using var textReader = new StringReader(json);
            using var jsonReader = new JsonTextReader(textReader);
            var result = serializer.Deserialize(jsonReader, objectType);
            return result;
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to deserialize object of type {objectType} from JSON string:\n{json}", e);
            throw;
        }
    }
    
    private static string SerializeToString(JsonSerializer serializer, object value)
    {
        try
        {
            using var textWriter = new StringWriter();
            using var jsonWriter = new JsonTextWriter(textWriter);
            serializer.Serialize(jsonWriter, value);
            return textWriter.ToString();
        }
        catch (Exception e)
        {
            Log.Warn($"Failed to serialize {(value == null ? "null" : $"object of type {value.GetType()}")} to JSON string{(value == null ? string.Empty : $": {value}")}", e);
            throw;
        }
    }
}