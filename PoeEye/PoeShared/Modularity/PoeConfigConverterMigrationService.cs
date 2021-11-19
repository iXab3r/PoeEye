﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PoeShared.Logging;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    internal sealed class PoeConfigConverterMigrationService : DisposableReactiveObject, IPoeConfigConverterMigrationService
    {
        private static readonly IFluentLog Log = typeof(PoeConfigConverterMigrationService).PrepareLogger();

        private readonly ConcurrentDictionary<PoeConfigMigrationConverterKey, Func<object, object>> convertersByMetadata = new();
        private readonly HashSet<Assembly> processedAssemblies = new();
        private readonly ConcurrentDictionary<Type, IPoeEyeConfigVersioned> versionedConfigByType = new();

        public bool AutomaticallyLoadConverters { get; set; } = true;

        public bool TryGetConverter(Type targetType, int sourceVersion, int targetVersion, out KeyValuePair<PoeConfigMigrationConverterKey, Func<object, object>> result)
        {
            var converterKvp = convertersByMetadata
                .FirstOrDefault(x => x.Key.TargetType == targetType && x.Key.SourceVersion == sourceVersion && x.Key.TargetVersion == targetVersion);
            if (converterKvp.Value != null)
            {
                result = converterKvp;
                return true;
            }
            else if (AutomaticallyLoadConverters)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                assemblies.Except(processedAssemblies).ForEach(LoadConvertersFromAssembly);
                return TryGetConverter(targetType, sourceVersion, targetVersion, out result);
            }

            result = default;
            return false;
        }

        public bool IsMetadataConverter(Type type)
        {
            if (type.IsAbstract)
            {
                return default;
            }

            var converterType = ResolveMetadataConverterType(type);
            return converterType != null;
        }

        public void RegisterMetadataConverter<T1, T2>(ConfigMetadataConverter<T1, T2> converter) where T1 : IPoeEyeConfigVersioned, new() where T2 : IPoeEyeConfigVersioned, new()
        {
            var typeV1 = typeof(T1);
            var typeV2 = typeof(T2);
            var assemblyV1 = typeV1.Assembly;
            var assemblyV2 = typeV2.Assembly;

            Log.Debug($"Registering converter {converter} for {typeV1}  => {typeV2}");
            var sampleV1 = versionedConfigByType.GetOrAdd(typeV1, _ => new T1());
            var sampleV2 = versionedConfigByType.GetOrAdd(typeV2, _ => new T2());
            Log.Debug($"Version of source type {typeV1} is {sampleV1.Version}, version of target type {typeV2} is {sampleV2.Version}");

            if (sampleV2.Version < sampleV1.Version)
            {
                throw new ArgumentException($"Source type {typeV1} version {sampleV1.Version} is higher than target type {typeV2} version {sampleV2.Version}");
            }

            if (assemblyV1 != assemblyV2)
            {
                throw new NotSupportedException($"Cross-assembly conversions are not supported, assembly for {typeV1} is {assemblyV1}, assembly for {typeV2} is {assemblyV2}");
            }

            var explicitConverterKey = new PoeConfigMigrationConverterKey
            {
                SourceType = typeV1,
                SourceVersion = sampleV1.Version,
                TargetType = typeV2,
                TargetVersion = sampleV2.Version
            };
            if (convertersByMetadata.TryGetValue(explicitConverterKey, out var existingConverter))
            {
                throw new InvalidOperationException($"Converter for {explicitConverterKey} is already registered: {existingConverter}");
            }
            Log.Debug($"Registering explicit converter: {explicitConverterKey}");

            Func<object, object> explicitConverter = src =>
            {
                if (src == null)
                {
                    throw new ArgumentException($"Converter to {typeof(T2)} expected non-null value of type {typeof(T1)}");
                }

                if (src is not T1 srcTyped)
                {
                    throw new ArgumentException($"Converter to {typeof(T2)} expected source of type {typeof(T1)}, got {src.GetType()}");
                }

                return converter.Convert(srcTyped);
            };
            convertersByMetadata[explicitConverterKey] = explicitConverter;
            RegisterImplicitConverters(explicitConverterKey);
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
            if (processedAssemblies.Contains(assembly))
            {
                return;
            }

            try
            {
                var registrationMethod = typeof(PoeConfigConverterMigrationService).GetMethod(nameof(RegisterMetadataConverter), BindingFlags.Instance | BindingFlags.Public)
                                         ?? throw new ApplicationException($"Failed to find registration method");
                var matchingTypes = assembly.GetTypes().Where(x => !x.IsAbstract).Select(x => new { InstanceType = x, ConverterType = ResolveMetadataConverterType(x) }).Where(x => x.ConverterType != null).ToArray();
                foreach (var converterType in matchingTypes)
                {
                    Log.Debug(() => $"Creating new converter: {converterType}");
                    var converter = Activator.CreateInstance(converterType.InstanceType);
                    var sourceConfigType = converterType.ConverterType.GetGenericArguments()[0];
                    var targetConfigType = converterType.ConverterType.GetGenericArguments()[1];
                    var registrationMethodTyped = registrationMethod.MakeGenericMethod(sourceConfigType, targetConfigType);
                    registrationMethodTyped.Invoke(this, new[] { converter });
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to load converters from assembly {assembly}", e);
            }
            finally
            {
                processedAssemblies.Add(assembly);
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
                convertersByMetadata[implicitConverterKey] = src =>
                {
                    Log.Debug($"Converting source using implicit {implicitConverterKey}");
                    var interimConversionResult = previousConverter.Value.Invoke(src);
                    Log.Debug($"Converting source using {converterKey}");
                    return explicitConverter(interimConversionResult);
                };
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
                convertersByMetadata[implicitConverterKey] = src =>
                {
                    Log.Debug($"Converting source using implicit {converterKey}");
                    var interimConversionResult = explicitConverter(src);
                    Log.Debug($"Converting source using {implicitConverterKey}");
                    return nextConverter.Value.Invoke(interimConversionResult);
                };
                RegisterImplicitConverters(implicitConverterKey);
            }
        }
    }
}