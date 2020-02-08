using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    internal sealed class PoeConfigConverter : JsonConverter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeConfigConverter));

        private static readonly MethodInfo ReloadConfigMethod = typeof(PoeConfigConverter)
            .GetMethod(nameof(GetMetadataTypedValue), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo SaveConfigMethod = typeof(PoeConfigConverter)
            .GetMethod(nameof(SetMetadataTypedValue), BindingFlags.Instance | BindingFlags.NonPublic);
        
        private readonly ConcurrentDictionary<Type, IPoeEyeConfigVersioned> versionedConfigByType = new ConcurrentDictionary<Type, IPoeEyeConfigVersioned>();
        private readonly ConcurrentDictionary<string, Assembly> loadedAssemblyByName = new ConcurrentDictionary<string, Assembly>();
        private readonly ConcurrentDictionary<Type, MethodInfo> getMetadataValueByType = new ConcurrentDictionary<Type, MethodInfo>();
        private readonly ConcurrentDictionary<Type, MethodInfo> setMetadataValueByType = new ConcurrentDictionary<Type, MethodInfo>();
        
        private bool skipNext = false;

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
                    var innerValueType = objectType.GetGenericArguments().Single();
                    var innerValue = GetMetadataValue(metadata);
                    if (innerValue != null)
                    {
                        metadata.ConfigValue = SerializeToToken(serializer, innerValue);
                    }
                }
            } else if (value is IPoeEyeConfig valueToSerialize)
            {
                metadata = new PoeConfigMetadata();
                metadata.AssemblyName = valueToSerialize.GetType().Assembly.GetName().Name;
                metadata.TypeName = valueToSerialize.GetType().FullName;
                metadata.Version = (value as IPoeEyeConfigVersioned)?.Version;
                metadata.ConfigValue = SerializeToToken(serializer, valueToSerialize);
            }
            else
            {
                throw new InvalidOperationException($"Failed to convert value {value} of type {value.GetType()} to {nameof(PoeConfigMetadata)}");
            }

            skipNext = true;
            serializer.Serialize(writer, metadata);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Guard.ArgumentIsTrue(() => typeof(IPoeEyeConfig).IsAssignableFrom(objectType));

            PoeConfigMetadata metadata;
            if (typeof(PoeConfigMetadata).IsAssignableFrom(objectType))
            {
                metadata = (PoeConfigMetadata)Deserialize(reader, serializer, objectType);
            }
            else
            {
                metadata = serializer.Deserialize<PoeConfigMetadata>(reader);
            }

            if (metadata == null)
            {
                Log.Warn($"Failed to convert type {objectType}, returning empty object instead");
                return null;
            }

            if (typeof(PoeConfigMetadata) == objectType)
            {
                return metadata;
            }
            
            if (!loadedAssemblyByName.TryGetValue(metadata.AssemblyName, out var assembly))
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == metadata.AssemblyName);
                if (assembly == null)
                {
                    Log.Warn($"Assembly {metadata.AssemblyName} is not loaded, could not convert type {metadata.TypeName} (version {(metadata.Version == null ? "is not set" : metadata.Version.ToString())}), returning wrapper object {metadata}");
                    return metadata;
                }

                loadedAssemblyByName[metadata.AssemblyName] = assembly;
            }
            
            var innerType = assembly.GetType(metadata.TypeName, throwOnError: false);
            if (innerType == null)
            {
                Log.Warn($"Failed to load Type {metadata.TypeName} (version {(metadata.Version == null ? "is not set" : metadata.Version.ToString())}) from assembly {assembly}, returning wrapper object {metadata}");
                return metadata;
            }

            var value = DeserializeInnerValue(metadata, serializer, innerType);
            
            if (typeof(PoeConfigMetadata).IsAssignableFrom(objectType) && objectType.IsGenericType)
            {
                SetMetadataValue(metadata, value);
                return metadata;
            }

            return value;
        }

        private object DeserializeInnerValue(PoeConfigMetadata metadata, JsonSerializer serializer, Type innerType)
        {
            if (typeof(IPoeEyeConfigVersioned).IsAssignableFrom(innerType))
            {
                var valueFactory = new Func<IPoeEyeConfigVersioned>(() => (IPoeEyeConfigVersioned) Activator.CreateInstance(innerType));
                var innerTypeSample = versionedConfigByType.GetOrAdd(innerType, _ => valueFactory());
                Log.Debug($"Validating config of type {innerType}, metadata version: {metadata.Version}, loaded in-memory version: {innerTypeSample.Version}");
                if (innerTypeSample.Version != metadata.Version)
                {
                    Log.Warn($"Config version mismatch (expected: {innerTypeSample.Version}, got: {metadata.Version})");
                    Log.Debug($"[ConfigProviderFromFile.ValidateConfigVersion] Loaded config:\n{metadata.DumpToText()}\n\nTemplate config:\n{innerTypeSample.DumpToText()}");
                    return valueFactory();
                }
            }
            
            return Deserialize(metadata.ConfigValue.ToString(), serializer, innerType);
        }
        
        private object Deserialize(JsonReader reader, JsonSerializer serializer, Type type)
        {
            skipNext = true;
            return serializer.Deserialize(reader, type);
        }
        
        private object Deserialize(string json, JsonSerializer serializer, Type type)
        {
            using (var textReader = new StringReader(json))
            {
                skipNext = true;
                return serializer.Deserialize(textReader, type);
            }
        }

        private JToken SerializeToToken(JsonSerializer serializer, object valueToSerialize)
        {
            using (var textWriter = new StringWriter())
            {
                skipNext = true;
                serializer.Serialize(textWriter, valueToSerialize);
                var serializedValue = textWriter.ToString();
                return JToken.Parse(serializedValue);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            var result = typeof(IPoeEyeConfig).IsAssignableFrom(objectType);
            if (result && skipNext)
            {
                skipNext = false;
                return false;
            }
            return result;
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
            invocationMethod.Invoke(this, new object[] { metadata, value });
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