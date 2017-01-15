namespace PoeBud.Config
{
    using System;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;

    using Guards;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    internal sealed class SafeDataConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Guard.ArgumentIsTrue(() => value is string);

            var bytesToEncode = Encoding.Default.GetBytes(value as string);
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
                return resultString;
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }
}