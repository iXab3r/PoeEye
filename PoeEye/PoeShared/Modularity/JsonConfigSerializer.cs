using System.Reactive;
using System.Reactive.Subjects;
using DynamicData;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace PoeShared.Modularity;

internal sealed class JsonConfigSerializer : IConfigSerializer
{
    private static readonly IFluentLog Log = typeof(JsonConfigSerializer).PrepareLogger();

    private readonly SourceListEx<JsonConverter> converters = new();
    private readonly int MaxCharsToLog = 1024;

    private JsonSerializerSettings jsonSerializerSettings;
    private readonly ISubject<ErrorContext> thrownExceptions = new Subject<ErrorContext>();

    public JsonConfigSerializer(PoeConfigConverter configConverter)
    {
        RegisterConverter(configConverter);
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
        var newSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
            Error = HandleSerializerError,
            NullValueHandling = NullValueHandling.Ignore,
        };
        converters.Items.ForEach(newSettings.Converters.Add);

        jsonSerializerSettings = newSettings;
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
        var log = Log.WithSuffix(metadata);
        if (metadata.Value == null)
        {
            log.Debug(() => $"Metadata does not contain a value, trying to re-serialize it");
            var serialized = Serialize(metadata);
            if (string.IsNullOrEmpty(serialized))
            {
                throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
            }
            log.Debug(() => $"Deserializing metadata again");
            var deserialized = Deserialize<PoeConfigMetadata<T>>(serialized);
            if (deserialized.Value != null)
            {
                log.Debug(() => $"Successfully restored value: {deserialized.Value}");
                metadata = deserialized;
            }
            else
            {
                log.Warn($"Failed to restore value");
            }
        }
            
        if (metadata.Value == null)
        {
            log.Debug(() => $"Metadata does not contain a valid value, preparing default");
            var defaultItem = defaultItemFactory(metadata);
            log.Debug(() => $"Returning default value: {defaultItem}");
            return defaultItem;
        }

        log.Debug(() => $"Returning value: {metadata.Value}");
        return metadata.Value;
    }
}