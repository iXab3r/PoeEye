using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;

using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PoeShared.Scaffolding;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace PoeShared.Modularity
{
    internal sealed class JsonConfigSerializer : IConfigSerializer
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(JsonConfigSerializer));

        private readonly SourceList<JsonConverter> converters = new SourceList<JsonConverter>();
        private readonly int MaxCharsToLog = 1024;

        private JsonSerializerSettings jsonSerializerSettings;
        private readonly ISubject<ErrorContext> thrownExceptions = new Subject<ErrorContext>();

        public JsonConfigSerializer()
        {
            RegisterConverter(new PoeConfigConverter());
            converters
                .Connect()
                .ToUnit()
                .StartWith(Unit.Default)
                .Subscribe(ReinitializeSerializerSettings);
        }

        public IObservable<ErrorContext> ThrownExceptions => thrownExceptions;

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
                    $"Could not deserialize data to instance of type {typeof(T)}, serialized data: \n{serializedData.Substring(0, Math.Min(MaxCharsToLog, serializedData.Length))}");
            }

            if (!(result is T))
            {
                throw new InvalidCastException($"Deserialized result of type {result.GetType()} is not assignable to type {typeof(T)}, result: {result}");
            }

            return (T) result;
        }

        public T[] DeserializeSingleOrList<T>(string serializedData)
        {
            using (var textReader = new StringReader(serializedData))
            using (var jsonReader = new JsonTextReader(textReader))
            {
                if (jsonReader.Read())
                {
                    switch (jsonReader.TokenType)
                    {
                        case JsonToken.StartArray:
                            return JsonConvert.DeserializeObject<T[]>(serializedData, jsonSerializerSettings);

                        case JsonToken.StartObject:
                            var instance = JsonConvert.DeserializeObject<T>(serializedData, jsonSerializerSettings);
                            return new [] { instance };
                    }
                }
                throw new FormatException(
                    $"Operation failed, could not deserialize data to instance of type {typeof(T)}, serialized data: \n{serializedData.Substring(0, Math.Min(MaxCharsToLog, serializedData.Length))}");
            }
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
                TypeNameHandling = TypeNameHandling.Auto,
                Error = HandleSerializerError,
                NullValueHandling = NullValueHandling.Ignore,
            };

            converters.Items.ForEach(jsonSerializerSettings.Converters.Add);
        }

        private void HandleSerializerError(object sender, ErrorEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }
            thrownExceptions.OnNext(args.ErrorContext);
        }
        
        public T DeserializeOrDefault<T>(
            PoeConfigMetadata<T> metadata, 
            Func<PoeConfigMetadata<T>, T> defaultItemFactory) where T : IPoeEyeConfig
        {
            if (metadata.Value == null)
            {
                Log.Debug($"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
                var serialized = Serialize(metadata);
                if (string.IsNullOrEmpty(serialized))
                {
                    throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
                }
                var deserialized = Deserialize<PoeConfigMetadata<T>>(serialized);
                if (deserialized.Value != null)
                {
                    Log.Debug($"Successfully restored type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}: {deserialized.Value}");
                    metadata = deserialized;
                }
                else
                {
                    Log.Warn($"Failed to restore type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}");
                }
            }
            
            if (metadata.Value == null)
            {
                return defaultItemFactory(metadata);
            }

            return metadata.Value;
        }
    }
}