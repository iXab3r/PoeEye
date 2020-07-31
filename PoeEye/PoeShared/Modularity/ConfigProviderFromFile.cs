using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using log4net;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    public sealed class ConfigProviderFromFile : DisposableReactiveObject, IConfigProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigProviderFromFile));

        private static readonly string DebugConfigFileName = @"configDebugMode.cfg";
        private static readonly string ReleaseConfigFileName = @"config.cfg";

        private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
        private readonly IConfigSerializer configSerializer;

        private readonly ConcurrentDictionary<string, IPoeEyeConfig> loadedConfigsByType = new ConcurrentDictionary<string, IPoeEyeConfig>();
        private string loadedConfigurationFile;

        public ConfigProviderFromFile(
            IConfigSerializer configSerializer,
            IAppArguments appArguments)
        {
            this.configSerializer = configSerializer;

            string configFileName;
            if (appArguments.IsDebugMode)
            {
                Log.Info("Debug mode detected");
                configFileName = DebugConfigFileName;
            }
            else
            {
                Log.Info($"Release mode detected");
                configFileName = ReleaseConfigFileName;
            }

            var candidates = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                appArguments.AppDataDirectory
            }
                .Select(x => Path.Combine(x, configFileName))
                .Select(x => new { Path = x, Exists = File.Exists(x) })
                .ToArray();
            Log.Debug($"Configuration matrix, configuration file name: {configFileName}:\n\t{candidates.DumpToTable()}");
            var existingFilePath = candidates.FirstOrDefault(x => x.Exists);
            if (existingFilePath != null)
            {
                ConfigFilePath = existingFilePath.Path;
                Log.Info($"Using existing configuration file @ {ConfigFilePath}");
            }
            else
            {
                ConfigFilePath = candidates.Last().Path;
                Log.Info($"Configuration file not found, using path {ConfigFilePath}");
            }

            if (string.IsNullOrEmpty(ConfigFilePath))
            {
                throw new ApplicationException($"Failed to get configuration file path");
            }

            configSerializer.ThrownExceptions
                .Subscribe(
                    errorContext =>
                    {
                        //FIXME Serializer errors should be treated appropriately, e.g. load value from default config on error
                        Log.Warn(
                            $"[PoeEyeConfigProviderFromFile.SerializerError] Suppressing serializer error ! Path: {errorContext.Path}, Member: {errorContext.Member}, Handled: {errorContext.Handled}",
                            errorContext.Error);
                        errorContext.Handled = true;
                    })
                .AddTo(Anchors);
        }
        
        public string ConfigFilePath { [NotNull] get; }

        public IObservable<Unit> ConfigHasChanged => configHasChanged;

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Reload()
        {
            Log.Debug("Reloading configuration...");

            var config = LoadInternal();

            config.Items
                .ToList()
                .ForEach(x =>
                {
                    string configType;
                    if (x is PoeConfigMetadata metadata)
                    {
                        configType = metadata.TypeName;
                    }
                    else
                    {
                        configType = x.GetType().FullName;
                    }
                    if (string.IsNullOrEmpty(configType))
                    {
                        throw new ApplicationException($"Could not determine FullName for config {config.GetType()}");
                    }
                    
                    loadedConfigsByType[configType] = x;
                });

            configHasChanged.OnNext(Unit.Default);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Save<TConfig>(TConfig config) where TConfig : IPoeEyeConfig, new()
        {
            var configType = config.GetType().FullName;
            if (string.IsNullOrEmpty(configType))
            {
                throw new ApplicationException($"Could not determine FullName for config {config.GetType()}");
            }
            loadedConfigsByType[configType] = config;
            Save();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Save()
        {
            var metaConfig = new PoeEyeCombinedConfig();
            loadedConfigsByType.Values.ToList().ForEach(x => metaConfig.Add(x));
            Log.Debug($"Saving all configs, metadata: { ObjectExtensions.DumpToTextRaw(metaConfig) }");

            SaveInternal(metaConfig);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
        {
            var configType = typeof(TConfig).FullName;
            if (string.IsNullOrEmpty(configType))
            {
                throw new ApplicationException($"Failed to get {nameof(Type.FullName)} of type {typeof(TConfig)}");
            }

            if (loadedConfigurationFile == null)
            {
                Log.Info($"Forcing to load initial configuration from file");
                Reload();
            }

            var config = loadedConfigsByType.GetOrAdd(configType, key => (TConfig) Activator.CreateInstance(typeof(TConfig)));

            if (config is PoeConfigMetadata metadata)
            {
                Log.Debug($"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
                var serialized = configSerializer.Serialize(metadata);
                if (string.IsNullOrEmpty(serialized))
                {
                    throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
                }
                var deserialized = configSerializer.Deserialize<TConfig>(serialized);
                loadedConfigsByType[configType] = deserialized;
                return deserialized;
            }
            
            return (TConfig) config;
        }

        private void SaveInternal(PoeEyeCombinedConfig config)
        {
            try
            {
                Log.Debug($"Saving config to file '{ConfigFilePath}'");
                Log.Debug("Serializing config data...");
                var serializedData = configSerializer.Serialize(config);

                Log.Debug($"Successfully serialized config, got {serializedData.Length} chars");

                Log.Debug($"Writing config data to file '{ConfigFilePath}'...");

                var directoryPath = Path.GetDirectoryName(ConfigFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(ConfigFilePath, serializedData, Encoding.Unicode);

                configHasChanged.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Log.Warn("Exception occurred, config was not saved correctly", ex);
            }
        }

        private PoeEyeCombinedConfig LoadInternal()
        {
            Log.Debug($"Loading config from file '{ConfigFilePath}'");
            loadedConfigsByType.Clear();
            loadedConfigurationFile = string.Empty;

            if (!File.Exists(ConfigFilePath))
            {
                Log.Debug($"File not found, fileName: '{ConfigFilePath}'");
                return new PoeEyeCombinedConfig();
            }

            PoeEyeCombinedConfig result = null;
            try
            {
                var fileData = File.ReadAllText(ConfigFilePath);
                Log.Debug($"Successfully read {fileData.Length} chars, deserializing...");
                loadedConfigurationFile = fileData;

                result = configSerializer.Deserialize<PoeEyeCombinedConfig>(fileData);
                Log.Debug("Successfully deserialized config data");
            }
            catch (Exception ex)
            {
                Log.Warn("Could not deserialize config data, default config will be used", ex);
                CreateBackupOfConfig();
            }

            return result ?? new PoeEyeCombinedConfig();
        }

        private void CreateBackupOfConfig()
        {
            try
            {
                if (!File.Exists(ConfigFilePath))
                {
                    return;
                }

                var backupFilePath = Path.Combine(
                    Path.GetDirectoryName(ConfigFilePath),
                    $"{ConfigFilePath}.bak");
                Log.Debug($"Creating a backup of existing config data '{ConfigFilePath}' to '{backupFilePath}' (backup exists: {File.Exists(backupFilePath)})");
                if (File.Exists(backupFilePath))
                {
                    Log.Debug($"Removing backup file {backupFilePath}...");
                    File.Delete(backupFilePath);
                }
                File.Move(ConfigFilePath, backupFilePath);
            }
            catch (Exception ex)
            {
                Log.Warn("Failed to create a backup", ex);
            }
        }

        private sealed class PoeEyeCombinedConfig : IPoeEyeConfigVersioned
        {
            private readonly ICollection<IPoeEyeConfig> items = new List<IPoeEyeConfig>();

            public int Version { get; set; } = 1;

            public IEnumerable<IPoeEyeConfig> Items => items;

            public PoeEyeCombinedConfig Add(IPoeEyeConfig item)
            {
                items.Add(item);
                return this;
            }
        }
    }
}