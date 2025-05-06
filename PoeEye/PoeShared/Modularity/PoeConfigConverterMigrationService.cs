using System.Reflection;
using PoeShared.Services;
using ReactiveUI;

namespace PoeShared.Modularity;

internal sealed class PoeConfigConverterMigrationService : DisposableReactiveObject, IPoeConfigConverterMigrationService
{
    private static readonly IFluentLog Log = typeof(PoeConfigConverterMigrationService).PrepareLogger();

    private static readonly MethodInfo RegistrationMethod = typeof(PoeConfigConverterMigrationService).GetMethod(nameof(RegisterMetadataConverter), BindingFlags.Instance | BindingFlags.Public) ??
                                                            throw new ApplicationException($"Failed to find registration method");

    private readonly Dictionary<PoeConfigMigrationConverterKey, ConverterData> convertersByMetadata = new();
    private readonly Dictionary<Type, IPoeEyeConfigVersioned> versionedConfigByType = new();
    private readonly NamedLock migrationsLock = new("ConfigMigrationServiceMigrations");

    private readonly ConcurrentQueue<Assembly> unprocessedAssemblies = new();

    public PoeConfigConverterMigrationService(IAssemblyTracker assemblyTracker)
    {
        Log.AddSuffix("Config Migrations");

        this.WhenAnyValue(x => x.AutomaticallyLoadConverters)
            .Select(x => x
                ? assemblyTracker.Assemblies.WhenAdded
                : Observable.Empty<Assembly>())
            .Switch()
            .Subscribe(x =>
            {
                Log.Debug($"Adding assembly to processing queue: {x}, size: {unprocessedAssemblies.Count}");
                unprocessedAssemblies.Enqueue(x);
            })
            .AddTo(Anchors);
    }
    
    public bool AutomaticallyLoadConverters { get; set; } = true;

    public void Clear()
    {
        using var @lock = migrationsLock.Enter();

        unprocessedAssemblies.Clear();
        convertersByMetadata.Clear();
        versionedConfigByType.Clear();
    }

    public bool TryGetConverter(Type targetType, int sourceVersion, int targetVersion, out PoeConfigMigrationConverter result)
    {
        using var @lock = migrationsLock.Enter();

        EnsureQueueIsProcessed();
        
        var logger = Log.WithSuffix($"{targetType} v{sourceVersion} => v{targetVersion}");
        logger.Debug($"Looking up converter");
        var converterKvp = convertersByMetadata
            .FirstOrDefault(x => x.Key.TargetType == targetType && x.Key.SourceVersion == sourceVersion && x.Key.TargetVersion == targetVersion);
        if (converterKvp.Value != null)
        {
            Log.Debug($"Found converter: {converterKvp}");
            result = new PoeConfigMigrationConverter {Key = converterKvp.Key, Converter = converterKvp.Value.ConverterFunc};
            return true;
        }

        Log.Debug($"Could not find matching converter");
        result = default;
        return false;
    }

    public bool IsMetadataConverter(Type type)
    {
        using var @lock = migrationsLock.Enter();
        if (type.IsAbstract)
        {
            return default;
        }

        var converterType = ResolveMetadataConverterType(type);
        return converterType != null;
    }

    public void RegisterMetadataConverter<T1, T2>(ConfigMetadataConverter<T1, T2> converter) where T1 : IPoeEyeConfigVersioned, new() where T2 : IPoeEyeConfigVersioned, new()
    {
        using var @lock = migrationsLock.Enter();
        
        EnsureQueueIsProcessed();
        
        var sourceType = typeof(T1);
        var targetType = typeof(T2);
        var assemblyV1 = sourceType.Assembly;
        var assemblyV2 = targetType.Assembly;

        Log.Debug($"Registering converter {converter} for {sourceType}  => {targetType}");
        var sourceSample = versionedConfigByType.GetOrAdd(sourceType, _ => new T1());
        var targetSample = versionedConfigByType.GetOrAdd(targetType, _ => new T2());
        Log.Debug($"Version of source type {sourceType} is {sourceSample.Version}, version of target type {targetType} is {targetSample.Version}");

        if (targetSample.Version < sourceSample.Version)
        {
            throw new ArgumentException($"Source type {sourceType} version {sourceSample.Version} is higher than target type {targetType} version {targetSample.Version}");
        }

        if (assemblyV1 != assemblyV2)
        {
            throw new NotSupportedException($"Cross-assembly conversions are not supported, assembly for {sourceType} is {assemblyV1}, assembly for {targetType} is {assemblyV2}");
        }

        var similarConverter = convertersByMetadata
            .FirstOrDefault(x => x.Key.TargetVersion == targetSample.Version && x.Key.SourceVersion == sourceSample.Version && x.Key.SourceType == sourceType);

        if (similarConverter.Value != null)
        {
            throw new ArgumentException($"There is already converter which converts {sourceType} (v{sourceSample.Version}) to {similarConverter.Key.TargetType} ({similarConverter.Key.TargetVersion}), not possible to register another one to {targetType} (v{targetSample.Version})");
        }
        
        var explicitConverterKey = new PoeConfigMigrationConverterKey
        {
            SourceType = sourceType,
            SourceVersion = sourceSample.Version,
            TargetType = targetType,
            TargetVersion = targetSample.Version
        };

        Log.Debug($"Registering explicit converter: {explicitConverterKey}");
        RegisterConverter(new ConverterData {Key = explicitConverterKey, ConverterFunc = ExplicitConvert});
        RegisterImplicitConverters(explicitConverterKey);
        return;
        
        object ExplicitConvert(object src)
        {
            if (src == null)
            {
                throw new ArgumentException($"Converter to {typeof(T2)} expected non-null value of type {typeof(T1)}");
            }

            if (src is not T1 srcTyped)
            {
                throw new ArgumentException($"Converter to {typeof(T2)} expected source of type {typeof(T1)}, got {src.GetType()}");
            }

            if (srcTyped.Version != explicitConverterKey.SourceVersion)
            {
                throw new InvalidStateException($"Failed to perform conversion - source version is wrong, expected {explicitConverterKey.SourceVersion}, got {srcTyped.Version}: {explicitConverterKey}");
            }

            var result = converter.Convert(srcTyped);
            if (result.Version != explicitConverterKey.TargetVersion)
            {
                throw new InvalidStateException($"Failed to perform conversion - converted version is wrong, expected {explicitConverterKey.TargetVersion}, got {result.Version}: {explicitConverterKey}");
            }

            return result;
        }
    }

    private void RegisterConverter(ConverterData converterData)
    {
        Log.Debug($"Adding new converter {converterData}");
        convertersByMetadata.AddOrUpdate(converterData.Key, () =>
        {
            Log.Debug($"Registered new converter successfully: {converterData}");
            return converterData;
        }, (_, existing) =>
        {
            Log.Warn($"Failed to register new converter {converterData}, already exists: {existing}");
            throw new ArgumentException($"Failed to register new converter {converterData}, already exists: {existing}");
        });
    }

    private void EnsureQueueIsProcessed()
    {
        while (unprocessedAssemblies.TryDequeue(out var assembly))
        {
            var hasConfigs = assembly.GetCustomAttribute<AssemblyHasPoeConfigConvertersAttribute>();
            if (hasConfigs == null)
            {
                continue;
            }
            Log.Info($"Detected unprocessed assemblies({unprocessedAssemblies.Count}), processing {assembly}");
            LoadConvertersFromAssembly(assembly);
        }
    }

    private static Type ResolveMetadataConverterType(Type type)
    {
        var genericTypeDef = type.IsGenericType ? type.GetGenericTypeDefinition() : default;
        if (genericTypeDef == typeof(ConfigMetadataConverter<,>))
        {
            return type;
        }

        if (type.BaseType == null)
        {
            return null;
        }

        return ResolveMetadataConverterType(type.BaseType);
    }

    private void LoadConvertersFromAssembly(Assembly assembly)
    {
        var logger = Log.WithSuffix(assembly.GetName().Name);
        logger.Debug("Loading converters from assembly");

        try
        {
            var matchingTypes = assembly.GetTypes().Where(x => !x.IsAbstract)
                .Select(x => new {InstanceType = x, ConverterType = ResolveMetadataConverterType(x)}).Where(x => x.ConverterType != null).ToArray();
            if (!matchingTypes.Any())
            {
                return;
            }
            logger.Debug($"Detected converters in assembly:\n\t{matchingTypes.DumpToTable()}");
            foreach (var converterType in matchingTypes)
            {
                logger.Debug($"Creating new converter: {converterType}");
                var converter = Activator.CreateInstance(converterType.InstanceType);
                var sourceConfigType = converterType.ConverterType.GetGenericArguments()[0];
                var targetConfigType = converterType.ConverterType.GetGenericArguments()[1];
                var registrationMethodTyped = RegistrationMethod.MakeGenericMethod(sourceConfigType, targetConfigType);
                registrationMethodTyped.Invoke(this, new[] {converter});
                logger.Debug($"Successfully registered converter {converter}");
            }
        }
        catch (Exception e)
        {
            logger.Warn($"Failed to load converters from assembly {new { assembly, assembly.Location }}", e);
        }
    }

    private void RegisterImplicitConverters(PoeConfigMigrationConverterKey converterKey)
    {
        // registering V1 => V2 and then V2 => V3 automatically creates V1 => V3
        // now we have V1 => V2, V2 => V3, V1 => V3
        // registering V3 to V4 automatically creates V1 => V4
        // V1 => V2, V2 => V3, V1 => V3 (implicit), V3 => V4, V1 => V4 (implicit)

        var previousConverter = convertersByMetadata.FirstOrDefault(x => x.Key.TargetType == converterKey.SourceType && x.Key.TargetVersion == converterKey.SourceVersion);
        if (previousConverter.Value != null)
        {
            var implicitConverterKey = new PoeConfigMigrationConverterKey
            {
                SourceType = previousConverter.Key.SourceType,
                SourceVersion = previousConverter.Key.SourceVersion,
                TargetType = converterKey.TargetType,
                TargetVersion = converterKey.TargetVersion,
                IsImplicit = true
            };
            if (convertersByMetadata.ContainsKey(implicitConverterKey))
            {
                return;
            }

            Log.Debug($"Registering implicit descending converter: {implicitConverterKey}");
            var explicitConverter = convertersByMetadata[converterKey];
            
            Func<object, object> descendingConverter = src =>
            {
                Log.Debug($"Converting source using implicit {implicitConverterKey}");
                var interimConversionResult = previousConverter.Value.ConverterFunc(src);
                Log.Debug($"Converting source using {converterKey}");
                return explicitConverter.ConverterFunc(interimConversionResult);
            };
            
            RegisterConverter(new ConverterData {Key = implicitConverterKey, ConverterFunc = descendingConverter, Comment = "Descending Implicit"});
            RegisterImplicitConverters(implicitConverterKey);
        }

        // registering V2 => V3 and then V1 => V2 automatically creates V1 => V3
        // now we have V2 => V3, V1 => V2, V1 => V3 (implicit)
        var nextConverter = convertersByMetadata.FirstOrDefault(x => x.Key.SourceType == converterKey.TargetType && x.Key.SourceVersion == converterKey.TargetVersion);
        if (nextConverter.Value != null)
        {
            var implicitConverterKey = new PoeConfigMigrationConverterKey
            {
                SourceType = converterKey.SourceType,
                SourceVersion = converterKey.SourceVersion,
                TargetType = nextConverter.Key.TargetType,
                TargetVersion = nextConverter.Key.TargetVersion,
                IsImplicit = true
            };
            if (convertersByMetadata.ContainsKey(implicitConverterKey))
            {
                return;
            }

            Log.Debug($"Registering implicit ascending converter: {implicitConverterKey}");
            var explicitConverter = convertersByMetadata[converterKey];
            
            Func<object, object> ascendingConverter = src =>
            {
                Log.Debug($"Converting source using implicit {converterKey}");
                var interimConversionResult = explicitConverter.ConverterFunc(src);
                Log.Debug($"Converting source using {implicitConverterKey}");
                return nextConverter.Value.ConverterFunc(interimConversionResult);
            };
            RegisterConverter(new ConverterData {Key = implicitConverterKey, ConverterFunc = ascendingConverter, Comment = "Ascending Implicit"});
            RegisterImplicitConverters(implicitConverterKey);
        }
    }

    private sealed record ConverterData
    {
        public PoeConfigMigrationConverterKey Key { get; init; }
        public string Comment { get; init; }
        public Func<object,object> ConverterFunc { get; init; }
    }
}