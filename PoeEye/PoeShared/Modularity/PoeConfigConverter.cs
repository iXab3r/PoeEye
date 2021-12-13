using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Logging;
using PoeShared.Prism;
using PoeShared.Scaffolding; 

namespace PoeShared.Modularity
{
    //FIXME This whole PoeConfigConverter is an awful mess that must be rewritten at some point
    internal sealed class PoeConfigConverter : JsonConverter
    {
        private static readonly IFluentLog Log = typeof(PoeConfigConverter).PrepareLogger();

        private static readonly MethodInfo ReloadConfigMethod = typeof(PoeConfigConverter)
            .GetMethod(nameof(GetMetadataTypedValue), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo SaveConfigMethod = typeof(PoeConfigConverter)
            .GetMethod(nameof(SetMetadataTypedValue), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly ConcurrentDictionary<Type, MethodInfo> getMetadataValueByType = new();
        private readonly IPoeConfigConverterMigrationService migrationService;
        private readonly ConcurrentDictionary<Type, MethodInfo> setMetadataValueByType = new();

        private readonly ConcurrentDictionary<Type, IPoeEyeConfigVersioned> versionedConfigByType = new();

        private volatile bool skipNext;

        public PoeConfigConverter(IPoeConfigConverterMigrationService migrationService)
        {
            this.migrationService = migrationService;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override bool CanConvert(Type objectType)
        {
            var isConfig = typeof(IPoeEyeConfig).IsAssignableFrom(objectType);
            if (isConfig && skipNext)
            {
                skipNext = false;
                return false;
            }
            return isConfig;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                return;
            }

            PoeConfigMetadata metadata;
            var objectType = value.GetType();
            if (typeof(PoeConfigMetadata).IsAssignableFrom(objectType))
            {
                metadata = (PoeConfigMetadata)value;
                if (objectType.IsGenericType)
                {
                    var innerValue = GetMetadataValue(metadata);
                    if (innerValue != null)
                    {
                        metadata.ConfigValue = SerializeToToken(serializer, innerValue);
                    }
                }
            } else if (value is IPoeEyeConfig valueToSerialize)
            {
                metadata = new PoeConfigMetadata
                {
                    AssemblyName = valueToSerialize.GetType().Assembly.GetName().Name,
                    TypeName = valueToSerialize.GetType().FullName,
                    Version = (value as IPoeEyeConfigVersioned)?.Version,
                    ConfigValue = SerializeToToken(serializer, valueToSerialize)
                };
            }
            else
            {
                throw new InvalidOperationException($"Failed to convert value {value} of type {value.GetType()} to {nameof(PoeConfigMetadata)}");
            }

            skipNext = true;
            serializer.Serialize(writer, metadata);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public override object ReadJson(JsonReader reader, Type serializedType, object existingValue, JsonSerializer serializer)
        {
            Guard.ArgumentIsTrue(() => typeof(IPoeEyeConfig).IsAssignableFrom(serializedType));

            var metadata = typeof(PoeConfigMetadata).IsAssignableFrom(serializedType)
                ? (PoeConfigMetadata) Deserialize(reader, serializer, serializedType)
                : serializer.Deserialize<PoeConfigMetadata>(reader);

            if (metadata == null)
            {
                Log.Warn(() => $"Failed to convert type {serializedType}, returning empty object instead");
                return null;
            }

            var isExactlyMetadataType = typeof(PoeConfigMetadata) == serializedType;
            if (isExactlyMetadataType)
            {
                return metadata;
            }

            var innerType = AssemblyHelper.Instance.ResolveType(metadata);
            if (innerType == null)
            {
                Log.Warn(() => $"Failed to load Type {metadata.TypeName} (version {(metadata.Version == null ? "is not set" : metadata.Version.ToString())}) from assembly {metadata.AssemblyName}, returning wrapper object {metadata}");
                return metadata;
            }
            
            var value = DeserializeMetadataValue(metadata, serializer, innerType);
            if (typeof(PoeConfigMetadata).IsAssignableFrom(serializedType) && serializedType.IsGenericType)
            {
                SetMetadataValue(metadata, value);
                return metadata;
            }

            return value;
        }

        private object DeserializeMetadataValue(PoeConfigMetadata metadata, JsonSerializer serializer, Type resolvedValueType)
        {
            if (!typeof(IPoeEyeConfigVersioned).IsAssignableFrom(resolvedValueType))
            {
                return Deserialize(metadata.ConfigValue.ToString(), serializer, resolvedValueType);
            }

            var valueFactory = new Func<IPoeEyeConfigVersioned>(() => (IPoeEyeConfigVersioned) Activator.CreateInstance(resolvedValueType));
            var innerTypeSample = versionedConfigByType.GetOrAdd(resolvedValueType, _ => valueFactory());
            Log.Debug(() => $"Validating config of type {resolvedValueType}, metadata version: {metadata.Version}, loaded in-memory version: {innerTypeSample.Version}");
            if (innerTypeSample.Version == metadata.Version)
            {
                return Deserialize(metadata.ConfigValue.ToString(), serializer, resolvedValueType);
            }

            Log.Warn(() => $"Config {metadata.TypeName} version mismatch (expected: {innerTypeSample.Version}, got: {metadata.Version})");

            if (metadata.Version != null)
            {
                Log.Debug(() => $"Looking up converter {metadata.TypeName} (v{metadata.Version}) => {resolvedValueType.FullName} (v{innerTypeSample.Version})");
                    
                if (migrationService.TryGetConverter(resolvedValueType, metadata.Version.Value, innerTypeSample.Version, out var converterKvp))
                {
                    Log.Debug(() => $"Found converter {converterKvp.Key}");
                    var sourceMetadata = new PoeConfigMetadata()
                    {
                        TypeName = converterKvp.Key.SourceType.FullName,
                        Version = converterKvp.Key.SourceVersion,
                        ConfigValue = metadata.ConfigValue,
                    };

                    var convertedValue = DeserializeMetadataValue(sourceMetadata, serializer, converterKvp.Key.SourceType);
                    Log.Debug(() => $"Deserialized config v{metadata.Version} into interim value of type {converterKvp.Key.SourceType} v{sourceMetadata.Version}");
                    try
                    {
                        var result = converterKvp.Value.Invoke(convertedValue);
                        Log.Debug(() => $"Successfully used converter {converterKvp.Key}");
                        return result;
                    }
                    catch (Exception e)
                    {
                        Log.Error($"Failed to convert intermediary value {convertedValue} ({sourceMetadata}) to {metadata}", e);
                        throw;
                    }
                }
            }
                
            Log.Warn(() => $"Using default of {resolvedValueType} v{innerTypeSample.Version} instead of {metadata}");
            return valueFactory();

        }

        private object Deserialize(JsonReader reader, JsonSerializer serializer, Type type)
        {
            skipNext = true;
            return serializer.Deserialize(reader, type);
        }

        private object Deserialize(string json, JsonSerializer serializer, Type type)
        {
            using var textReader = new StringReader(json);
            skipNext = true;
            return serializer.Deserialize(textReader, type);
        }

        private JToken SerializeToToken(JsonSerializer serializer, object valueToSerialize)
        {
            using var textWriter = new StringWriter();
            skipNext = true;
            serializer.Serialize(textWriter, valueToSerialize);
            var serializedValue = textWriter.ToString();
            return JToken.Parse(serializedValue);
        }

        private object GetMetadataValue(PoeConfigMetadata metadata)
        {
            var metadataType = metadata.GetType().GetGenericArguments().Single();
            var invocationMethod = getMetadataValueByType.GetOrAdd(metadataType, x => ReloadConfigMethod.MakeGenericMethod(x));
            return invocationMethod.Invoke(this, new object[] { metadata });
        }

        private void SetMetadataValue(PoeConfigMetadata metadata, object value)
        {
            var metadataType = metadata.GetType().GetGenericArguments().Single();
            var invocationMethod = setMetadataValueByType.GetOrAdd(metadataType, x => SaveConfigMethod.MakeGenericMethod(x));
            invocationMethod.Invoke(this, new[] { metadata, value });
        }

        private TConfig GetMetadataTypedValue<TConfig>(PoeConfigMetadata<TConfig> metadata)
            where TConfig : IPoeEyeConfig
        {
            return metadata.Value;
        }

        private void SetMetadataTypedValue<TConfig>(PoeConfigMetadata<TConfig> metadata, TConfig value)
            where TConfig : IPoeEyeConfig
        {
            metadata.Value = value;
        }
    }
}