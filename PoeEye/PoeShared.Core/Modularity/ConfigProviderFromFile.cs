using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using log4net;
using PoeShared.Scaffolding;

namespace PoeShared.Modularity
{
    public sealed class ConfigProviderFromFile : DisposableReactiveObject, IConfigProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ConfigProviderFromFile));

        private static readonly string ConfigFileDirectory = Path.Combine(AppArguments.Instance.AppDataDirectory);

        private static readonly string DebugConfigFileName = @"configDebugMode.cfg";
        private static readonly string ReleaseConfigFileName = @"config.cfg";

        private readonly string configFilePath;

        private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
        private readonly IConfigSerializer configSerializer;

        private readonly ConcurrentDictionary<string, IPoeEyeConfig> loadedConfigsByType = new ConcurrentDictionary<string, IPoeEyeConfig>();

        public ConfigProviderFromFile(IConfigSerializer configSerializer)
        {
            this.configSerializer = configSerializer;
            if (AppArguments.Instance.IsDebugMode)
            {
                Log.Info("Debug mode detected");
                configFilePath = Path.Combine(ConfigFileDirectory, DebugConfigFileName);
            }
            else
            {
                Log.Info("Release mode detected");
                configFilePath = Path.Combine(ConfigFileDirectory, ReleaseConfigFileName);
            }

            configSerializer.ThrownExceptions
                .Subscribe(
                    errorContext =>
                    {
                        //FIXME Serializer errors should be treated appropriately, e.g. load value from default config on error
                        Log.Warn(
                            $"[PoeEyeConfigProviderFromFile.SerializerError] Suppresing serializer error ! Path: {errorContext.Path}, Member: {errorContext.Member}, Handled: {errorContext.Handled}",
                            errorContext.Error);
                        errorContext.Handled = true;
                    })
                .AddTo(Anchors);
        }

        public IObservable<Unit> ConfigHasChanged => configHasChanged;

        public void Reload()
        {
            Log.Debug("Reloading configuration...");

            var config = LoadInternal();
            loadedConfigsByType.Clear();

            config.Items
                .ToList()
                .ForEach(x =>
                {
                    var configType = x.GetType().FullName;
                    if (string.IsNullOrEmpty(configType))
                    {
                        throw new ApplicationException($"Could not determine FullName for config {config.GetType()}");
                    }
                    
                    loadedConfigsByType[configType] = x;
                });

            configHasChanged.OnNext(Unit.Default);
        }

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

        public void Save()
        {
            var metaConfig = new PoeEyeCombinedConfig();
            loadedConfigsByType.Values.ToList().ForEach(x => metaConfig.Add(x));
            Log.Debug($"Saving all configs, metadata: { ObjectExtensions.DumpToTextRaw(metaConfig) }");

            SaveInternal(metaConfig);
        }

        public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
        {
            if (loadedConfigsByType.IsEmpty)
            {
                Reload();
            }

            return (TConfig) loadedConfigsByType.GetOrAdd(typeof(TConfig).FullName, key => (TConfig) Activator.CreateInstance(typeof(TConfig)));
        }

        private void SaveInternal(PoeEyeCombinedConfig config)
        {
            try
            {
                Log.Debug($"Saving config to file '{configFilePath}'");
                Log.Info($"Saving config to '{Path.GetFileName(configFilePath)}'");

                Log.Debug("Serializing config data...");
                var serializedData = configSerializer.Serialize(config);

                Log.Debug($"Successfully serialized config, got {serializedData.Length} chars");

                Log.Debug($"Writing config data to file '{configFilePath}'...");

                var directoryPath = Path.GetDirectoryName(configFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                File.WriteAllText(configFilePath, serializedData, Encoding.Unicode);

                configHasChanged.OnNext(Unit.Default);
            }
            catch (Exception ex)
            {
                Log.Warn("Exception occurred, config was not saved correctly", ex);
            }
        }

        private PoeEyeCombinedConfig LoadInternal()
        {
            Log.Debug($"Loading config from file '{configFilePath}'");
            Log.Info($"Loading config from '{Path.GetFileName(configFilePath)}'");
            loadedConfigsByType.Clear();

            if (!File.Exists(configFilePath))
            {
                Log.Debug($"File not found, fileName: '{configFilePath}'");
                return new PoeEyeCombinedConfig();
            }

            PoeEyeCombinedConfig result = null;
            try
            {
                var fileData = File.ReadAllText(configFilePath);
                Log.Debug($"Successfully read {fileData.Length} chars, deserializing...");

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
                if (!File.Exists(configFilePath))
                {
                    return;
                }

                var backupFileName = Path.Combine(
                    Path.GetDirectoryName(configFilePath),
                    $"{Path.GetFileNameWithoutExtension(configFilePath)}.bak{Path.GetExtension(configFilePath)}");
                Log.Debug($"Creating a backup of existing config data '{configFilePath}' to '{backupFileName}'");
                File.Copy(configFilePath, backupFileName);
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