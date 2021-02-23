using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using PoeShared.Scaffolding;

namespace PoeShared.Converters
{
    public sealed class CompressedDataConverter : JsonConverter
    {
        private static readonly string GzipPrefix = "GZip ";
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is byte[] valueToEncode))
            {
                throw new ArgumentException($"Expected instance of {typeof(byte[])}, got {value?.GetType()}");
            }
            
            var compressedData = Compress(valueToEncode);
            var compressedDataString = JsonConvert.SerializeObject(compressedData);

            var serializedString = (GzipPrefix + compressedDataString.Trim('"')).SurroundWith('"');
            writer.WriteRawValue(serializedString);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var deserializedString = serializer.Deserialize<string>(reader);
            
            if (deserializedString.StartsWith(GzipPrefix))
            {
                var compressedDataString = deserializedString.Substring(GzipPrefix.Length);
                var compressedData = JsonConvert.DeserializeObject<byte[]>(compressedDataString.SurroundWith('"'));
                if (compressedData != null)
                {
                    return Decompress(compressedData);
                }
            }
            else
            {
                return JsonConvert.DeserializeObject<byte[]>(deserializedString.SurroundWith('"'));
            }

            return null;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SecureString);
        }
        
        private static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        private static byte[] Decompress(byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}