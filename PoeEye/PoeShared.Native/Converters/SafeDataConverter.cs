using System;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Converters
{
    [SupportedOSPlatform("windows")]
    public sealed class SafeDataConverter : JsonConverter
    {
        private static readonly IFluentLog Log = typeof(SafeDataConverter).PrepareLogger();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is SecureString secureString))
            {
                throw new ArgumentException($"Expected instance of {nameof(SecureString)}, got {value?.GetType()}");
            }
            var bytesToEncode = Encoding.Default.GetBytes(secureString.ToUnsecuredString());
            var encodedBytes = ProtectedData.Protect(bytesToEncode, null, DataProtectionScope.LocalMachine);
            var serializedBytes = JsonConvert.SerializeObject(encodedBytes);
            writer.WriteRawValue(serializedBytes);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var deserializedBytes = serializer.Deserialize<byte[]>(reader);

            try
            {
                if (deserializedBytes != null)
                {
                    var decodedBytes = ProtectedData.Unprotect(deserializedBytes, null, DataProtectionScope.LocalMachine);
                    var resultString = Encoding.Default.GetString(decodedBytes);
                    return resultString.ToSecuredString();
                }
            }
            catch (CryptographicException e)
            {
                Log.Warn($"Failed to decrypt value {existingValue} into type {objectType}", e);
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SecureString);
        }
    }
}