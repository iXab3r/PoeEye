using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization.Formatters;
using DynamicData;
using Guards;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Modularity
{
    internal class JsonConfigSerializer : IConfigSerializer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(JsonConfigSerializer));

        private readonly SourceList<JsonConverter> converters = new SourceList<JsonConverter>();
        private readonly int MaxCharsToLog = 1024;

        private JsonSerializerSettings jsonSerializerSettings;

        public JsonConfigSerializer()
        {
            converters
                .Connect()
                .ToUnit()
                .StartWith(Unit.Default)
                .Subscribe(ReinitializeSerializerSettings);
        }

        public void RegisterConverter(JsonConverter converter)
        {
            Guard.ArgumentNotNull(converter, nameof(converter));

            converters.Add(converter);
        }

        public string Serialize(object data)
        {
            Guard.ArgumentNotNull(() => data);

            return JsonConvert.SerializeObject(data, jsonSerializerSettings);
        }

        public T Deserialize<T>(string serializedData)
        {
            Guard.ArgumentNotNullOrEmpty(() => serializedData);

            var result = JsonConvert.DeserializeObject(serializedData, typeof(T), jsonSerializerSettings);
            if (result == null)
            {
                throw new FormatException(
                    $"Operation failed, could not deserialize data to instance of type {typeof(T)}, serialized data: \n{serializedData.Substring(0, Math.Min(MaxCharsToLog, serializedData.Length))}");
            }

            return (T) result;
        }

        public string Compress(object data)
        {
            Guard.ArgumentNotNull(() => data);

            var serialized = Serialize(data);
            return StringExtensions.CompressStringToGZip(serialized);
        }

        public T Decompress<T>(string compressedData)
        {
            Guard.ArgumentNotNullOrEmpty(() => compressedData);

            var serialized = StringExtensions.DecompressStringFromGZip(compressedData);
            return Deserialize<T>(serialized);
        }

        private void ReinitializeSerializerSettings()
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                Error = HandleSerializerError
            };

            converters.Items.ForEach(jsonSerializerSettings.Converters.Add);
        }

        private void HandleSerializerError(object sender, ErrorEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }

            //FIXME Serializer errors should be treated appropriately, e.g. load value from default config on error
            Log.Warn(
                $"[PoeEyeConfigProviderFromFile.SerializerError] Suppresing serializer error ! Path: {args.ErrorContext.Path}, Member: {args.ErrorContext.Member}, Handled: {args.ErrorContext.Handled}",
                args.ErrorContext.Error);
            args.ErrorContext.Handled = true;
        }
    }
}