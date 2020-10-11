using System;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;
using PoeShared.Scaffolding;

namespace PoeShared.Converters
{
    [SupportedOSPlatform("windows")]
    public sealed class SafeDataConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Guard.ArgumentIsTrue(() => value is SecureString);

            var secureString = value as SecureString;
            var bytesToEncode = Encoding.Default.GetBytes(secureString.ToUnsecuredString());
            var encodedBytes = ProtectedData.Protect(bytesToEncode, null, DataProtectionScope.LocalMachine);
            var serializedBytes = JsonConvert.SerializeObject(encodedBytes);
            writer.WriteRawValue(serializedBytes);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var deserializedBytes = serializer.Deserialize<byte[]>(reader);

            if (deserializedBytes != null)
            {
                var decodedBytes = ProtectedData.Unprotect(deserializedBytes, null, DataProtectionScope.LocalMachine);
                var resultString = Encoding.Default.GetString(decodedBytes);
                return resultString.ToSecuredString();
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SecureString);
        }
    }
}