using System.Buffers;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using DynamicData;
using Newtonsoft.Json;
using PoeShared.Services;
using Unity;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace PoeShared.Modularity;

internal sealed class JsonConfigSerializer : DisposableReactiveObjectWithLogger, IConfigSerializer
{
    private readonly SourceListEx<JsonConverter> converters = new();
    private readonly int MaxCharsToLog = 1024;

    private JsonSerializerSettings jsonSerializerSettings;
    private JsonSerializer jsonSerializer;

    public JsonConfigSerializer([OptionalDependency] params JsonConverter[] defaultConverters)
    {
        if (defaultConverters != null)
        {
            converters.AddRange(defaultConverters);
        }
        converters
            .Connect()
            .ToUnit()
            .StartWith(Unit.Default)
            .Subscribe(ReinitializeSerializerSettings)
            .AddTo(Anchors);
    }

    public void RegisterConverter(JsonConverter converter)
    {
        converters.Add(converter);
    }

    public void Serialize(object data, FileInfo file)
    {
        using var writer = new StreamWriter(file.FullName);
        Serialize(data, writer);
    }

    public void Serialize(object data, TextWriter textWriter)
    {
        using var jsonWriter = CreateWriter(textWriter);
        jsonSerializer.Serialize(jsonWriter, data);
    }
    
    public string Serialize(object data)
    {
        using var stringWriter = new StringWriter();
        Serialize(data, stringWriter);
        return stringWriter.ToString();
    }

    public T Deserialize<T>(FileInfo file)
    {
        using var stringReader = new StreamReader(file.FullName);
        return Deserialize<T>(stringReader);
    }
    
    public T Deserialize<T>(TextReader textReader)
    {
        using var jsonReader = CreateReader(textReader);

        var result = jsonSerializer.Deserialize(jsonReader, typeof(T));
        if (result == null)
        {
            throw new FormatException(
                $"Could not deserialize data to instance of type {typeof(T)} from data stream");
        }

        if (!(result is T))
        {
            throw new InvalidCastException($"Deserialized result of type {result.GetType()} is not assignable to type {typeof(T)}, result: {result}");
        }

        return (T) result;
    }
    
    public T Deserialize<T>(string serializedData)
    {
        using var stringReader = new StringReader(serializedData);
        try
        {
            return Deserialize<T>(stringReader);
        }
        catch (FormatException e)
        {
            throw new FormatException($"Could not deserialize data to instance of type {typeof(T)}, serialized data: \\n{serializedData.Substring(0, Math.Min(MaxCharsToLog, serializedData.Length))}", e);
        }
    }

    public T[] DeserializeSingleOrList<T>(string serializedData)
    {
        using (var textReader = new StringReader(serializedData))
        using (var jsonReader = CreateReader(textReader))
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
        var serialized = Serialize(data);
        return StringUtils.CompressStringToGZip(serialized);
    }

    public T Decompress<T>(string compressedData)
    {
        var serialized = StringUtils.DecompressStringFromGZip(compressedData);
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
        newSettings.ContractResolver = new PoeSharedContractResolver();
        converters.Items.ForEach(newSettings.Converters.Add);

        jsonSerializerSettings = newSettings;
        jsonSerializer = JsonSerializer.Create(newSettings);
    }

    private void HandleSerializerError(object sender, ErrorEventArgs args)
    {
        if (sender == null || args == null)
        {
            return;
        }

        var errorMessage = $"Serializer encountered error: { new { sender, args.CurrentObject, args.ErrorContext.OriginalObject, args.ErrorContext.Path, args.ErrorContext.Member, args.ErrorContext.Handled } }";
        Log.Error(errorMessage, args.ErrorContext.Error);
    }
        
    public T DeserializeOrDefault<T>(
        PoeConfigMetadata<T> metadata, 
        Func<PoeConfigMetadata<T>, T> defaultItemFactory) where T : class
    {
        var log = Log.WithSuffix(metadata);
        if (metadata.Value == null)
        {
            log.Debug($"Metadata does not contain a value, trying to re-serialize it");
            var serialized = Serialize(metadata);
            if (string.IsNullOrEmpty(serialized))
            {
                throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
            }
            log.Debug($"Deserializing metadata again");
            var deserialized = Deserialize<PoeConfigMetadata<T>>(serialized);
            if (deserialized.Value != null)
            {
                log.Debug($"Successfully restored value: {deserialized.Value}");
                metadata = deserialized;
            }
            else
            {
                log.Warn($"Failed to restore value");
            }
        }
            
        if (metadata.Value == null)
        {
            log.Debug($"Metadata does not contain a valid value, preparing default");
            var defaultItem = defaultItemFactory(metadata);
            log.Debug($"Returning default value: {defaultItem}");
            return defaultItem;
        }

        log.Debug($"Returning value: {metadata.Value}");
        return metadata.Value;
    }
    
    private static JsonTextReader CreateReader(TextReader reader)
    {
        return new JsonTextReader(reader)
        {
            ArrayPool = SharedArrayPool<char>.Instance
        };
    }
    
    private static JsonTextWriter CreateWriter(TextWriter writer)
    {
        return new JsonTextWriter(writer)
        {
            ArrayPool = SharedArrayPool<char>.Instance
        };
    }
    
    public class FakeArrayPool<T> : LazyReactiveObject<FakeArrayPool<T>>, IArrayPool<T>
    {
        private static readonly IFluentLog Log = typeof(FakeArrayPool<T>).PrepareLogger();

        public T[] Rent(int minimumLength)
        {
            Log.Info($"Renting array, min length: {minimumLength}");
            var result = new T[minimumLength];
            Log.Info($"Array rented, requested: {minimumLength}, got: {result.Length}");
            return result;
        }

        public void Return(T[] array)
        {
            Log.Info($"Returning array, length: {array.Length}");
            Log.Info($"Returned array, length: {array.Length}");
        }
    }
}