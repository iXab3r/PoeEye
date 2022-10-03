using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PoeShared.Modularity;

//FIXME This whole PoeConfigConverter is an awful mess that must be rewritten at some point
internal sealed class PoeConfigConverter : JsonConverter
{
    private static readonly IFluentLog Log = typeof(PoeConfigConverter).PrepareLogger();

    private static readonly MethodInfo GetMetadataValueMethod = typeof(PoeConfigConverter)
                                                                    .GetMethod(nameof(GetMetadataTypedValue), BindingFlags.Static | BindingFlags.NonPublic)
                                                                ?? throw new MissingMethodException($"Failed to find method {nameof(GetMetadataTypedValue)} in type {typeof(PoeConfigConverter)}");

    private static readonly MethodInfo SetMetadataValueMethod = typeof(PoeConfigConverter)
                                                                    .GetMethod(nameof(SetMetadataTypedValue), BindingFlags.Static | BindingFlags.NonPublic)
                                                                ?? throw new MissingMethodException($"Failed to find method {nameof(SetMetadataTypedValue)} in type {typeof(PoeConfigConverter)}"); 
        
    private static readonly ConcurrentDictionary<Type, Func<PoeConfigMetadata, object>> GetMetadataValueByType = new();
    private static readonly ConcurrentDictionary<Type, Action<PoeConfigMetadata, object>> SetMetadataValueByType = new();
    private static readonly ConcurrentDictionary<Type, IPoeEyeConfigVersioned> VersionedConfigByType = new();

    private readonly IPoeConfigMetadataReplacementService replacementService;
    private readonly IPoeConfigConverterMigrationService migrationService;

    private volatile bool skipNext;

    public PoeConfigConverter(
        IPoeConfigMetadataReplacementService replacementService,
        IPoeConfigConverterMigrationService migrationService)
    {
        this.replacementService = replacementService;
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

        var metadata = PrepareMetadata(value, serializer);
        skipNext = true;
        serializer.Serialize(writer, metadata);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public override object ReadJson(JsonReader reader, Type serializedType, object existingValue, JsonSerializer serializer)
    {
        Guard.ArgumentIsTrue(() => typeof(IPoeEyeConfig).IsAssignableFrom(serializedType));

        var metadata = DeserializeMetadata(reader, serializedType, serializer);
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
            Log.Warn(() => $"Failed to load Type {metadata.TypeName} (version {(metadata.Version == null ? "is not set" : metadata.Version.ToString())}) from assembly {metadata.AssemblyName}, returning wrapper object {new { metadata.TypeName, metadata.Version }}");
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

    private PoeConfigMetadata PrepareMetadata(object value, JsonSerializer serializer)
    {
        var objectType = value.GetType();
        if (typeof(PoeConfigMetadata).IsAssignableFrom(objectType))
        {
            var metadata = (PoeConfigMetadata)value;
            if (!objectType.IsGenericType)
            {
                return metadata;
            }

            var innerValue = GetMetadataValue(metadata);
            if (innerValue != null)
            {
                metadata.ConfigValue = SerializeToToken(serializer, innerValue);
            }

            return metadata;
        }

        if (value is IPoeEyeConfig valueToSerialize)
        {
            return new PoeConfigMetadata
            {
                AssemblyName = valueToSerialize.GetType().Assembly.GetName().Name,
                TypeName = valueToSerialize.GetType().FullName,
                Version = (value as IPoeEyeConfigVersioned)?.Version,
                ConfigValue = SerializeToToken(serializer, valueToSerialize)
            };
        }

        throw new InvalidOperationException($"Failed to convert value {value} of type {value.GetType()} to {nameof(PoeConfigMetadata)}");
    }

    private PoeConfigMetadata DeserializeMetadata(JsonReader reader, Type serializedType, JsonSerializer serializer)
    {
        var metadata = typeof(PoeConfigMetadata).IsAssignableFrom(serializedType)
            ? (PoeConfigMetadata) Deserialize(reader, serializer, serializedType)
            : serializer.Deserialize<PoeConfigMetadata>(reader);
        var result = replacementService.ReplaceIfNeeded(metadata);
        if (result == null)
        {
            throw new ArgumentException($"Replacement service returned null for metadata: {metadata}");
        }
        return result;
    }

    private object DeserializeMetadataValue(PoeConfigMetadata metadata, JsonSerializer serializer, Type resolvedValueType)
    {
        if (!typeof(IPoeEyeConfigVersioned).IsAssignableFrom(resolvedValueType))
        {
            return DeserializeFromToken(metadata.ConfigValue, serializer, resolvedValueType);
        }

        var valueFactory = new Func<IPoeEyeConfigVersioned>(() => (IPoeEyeConfigVersioned) Activator.CreateInstance(resolvedValueType));
        var innerTypeSample = VersionedConfigByType.GetOrAdd(resolvedValueType, _ => valueFactory());
        Log.Debug(() => $"Validating config of type {resolvedValueType}, metadata version: {metadata.Version}, loaded in-memory version: {innerTypeSample.Version}");
        if (innerTypeSample.Version == metadata.Version)
        {
            return DeserializeFromToken(metadata.ConfigValue, serializer, resolvedValueType);
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
                    TypeName = converterKvp.Key.LegacyType.FullName,
                    Version = converterKvp.Key.SourceVersion,
                    ConfigValue = metadata.ConfigValue,
                };

                var convertedValue = DeserializeMetadataValue(sourceMetadata, serializer, converterKvp.Key.LegacyType);
                Log.Debug(() => $"Deserialized config v{metadata.Version} into interim value of type {converterKvp.Key.LegacyType} v{sourceMetadata.Version}");
                try
                {
                    var result = converterKvp.Converter(convertedValue);
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

    private object DeserializeFromToken(JToken token, JsonSerializer serializer, Type type)
    {
        using var tokenReader = new JTokenReader(token);
        skipNext = true;
        return serializer.Deserialize(tokenReader, type);
    }

    private JToken SerializeToToken(JsonSerializer serializer, object valueToSerialize)
    {
        using var tokenWriter = new JTokenWriter();
        skipNext = true;
        serializer.Serialize(tokenWriter, valueToSerialize);
        return tokenWriter.Token;
    }

    private static object GetMetadataValue(PoeConfigMetadata metadata)
    {
        var metadataType = metadata.GetType().GetGenericArguments().Single();
        var invocationMethod = GetMetadataValueByType.GetOrAdd(metadataType, PrepareGetMetadataTypedValue);
        return invocationMethod(metadata);
    }

    private static void SetMetadataValue(PoeConfigMetadata metadata, object value)
    {
        var metadataType = metadata.GetType().GetGenericArguments().Single();
        var invocationMethod = SetMetadataValueByType.GetOrAdd(metadataType, PrepareSetMetadataTypedValue);
        invocationMethod(metadata, value);
    }
        
    private static Func<PoeConfigMetadata, object> PrepareGetMetadataTypedValue(Type configType)
    {
        var inputParameter = Expression.Parameter(typeof(PoeConfigMetadata), "input");

        var method = GetMetadataValueMethod.MakeGenericMethod(configType);
        var methodExpr = Expression.Call(method, Expression.Convert(inputParameter, typeof(PoeConfigMetadata<>).MakeGenericType(configType)));

        var lambda = Expression.Lambda<Func<PoeConfigMetadata, object>>(methodExpr, inputParameter);
        return PropertyBinder.Binder.ExpressionCompiler.Compile(lambda);
    }
        
    private static Action<PoeConfigMetadata, object> PrepareSetMetadataTypedValue(Type configType)
    {
        var inputParameter = Expression.Parameter(typeof(PoeConfigMetadata), "input");
        var valueParameter = Expression.Parameter(typeof(object), "configValue");

        var method = SetMetadataValueMethod.MakeGenericMethod(configType);
        var methodExpr = Expression.Call(
            method, 
            Expression.Convert(inputParameter, typeof(PoeConfigMetadata<>).MakeGenericType(configType)), 
            Expression.Convert(valueParameter, configType));

        var lambda = Expression.Lambda<Action<PoeConfigMetadata, object>>(methodExpr, inputParameter, valueParameter);
        return PropertyBinder.Binder.ExpressionCompiler.Compile(lambda);
    }
        
    private static TConfig GetMetadataTypedValue<TConfig>(PoeConfigMetadata<TConfig> metadata)
        where TConfig : IPoeEyeConfig
    {
        return metadata.Value;
    }

    private static void SetMetadataTypedValue<TConfig>(PoeConfigMetadata<TConfig> metadata, TConfig value)
        where TConfig : IPoeEyeConfig
    {
        metadata.Value = value;
    }
}