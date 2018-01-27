using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Scaffolding;
using ReactiveUI;
using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace PoeEye.Config
{
    internal sealed class PoeEyeConfigProviderFromFile : IConfigProvider
    {
        private static readonly string ConfigFileDirectory = AppArguments.AppDataDirectory;
        
        private static readonly string DebugConfigFileName = @"configDebugMode.cfg";
        private static readonly string ReleaseConfigFileName = @"config.cfg";

        private readonly string configFilePath;

        private JsonSerializerSettings jsonSerializerSettings;
        private readonly IReactiveList<JsonConverter> converters = new ReactiveList<JsonConverter>();
        private readonly ConcurrentDictionary<string, IPoeEyeConfig> loadedConfigs = new ConcurrentDictionary<string, IPoeEyeConfig>();

        private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();

        public PoeEyeConfigProviderFromFile()
        {
            if (AppArguments.Instance.IsDebugMode)
            {
                Log.Instance.Debug("[PoeEyeConfigProviderFromFile..ctor] Debug mode detected");
                configFilePath = Path.Combine(ConfigFileDirectory, DebugConfigFileName);
            }
            else
            {
                Log.Instance.Debug("[PoeEyeConfigProviderFromFile..ctor] Release mode detected");
                configFilePath = Path.Combine(ConfigFileDirectory, ReleaseConfigFileName);
            }

            converters.Changed
                .ToUnit()
                .StartWith(Unit.Default)
                .Subscribe(ReinitializeSerializerSettings);
        }

        public IObservable<Unit> ConfigHasChanged => configHasChanged;

        public void Reload()
        {
            Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Reload] Reloading configuration...");

            var config = LoadInternal();
            loadedConfigs.Clear();

            config.Items
                .ToList()
                .Select(x => x.Content)
                .Select(ValidateConfigVersion)
                .ForEach(x => loadedConfigs[x.GetType().FullName] = x);

            configHasChanged.OnNext(Unit.Default);
        }
        
        public void Save<TConfig>(TConfig config) where TConfig : IPoeEyeConfig, new()
        {
            var key = new PoeEyeConfigMetadata(config);
            loadedConfigs[key.ConfigTypeName] = config;

            var metaConfig = new PoeEyeCombinedConfig();
            loadedConfigs.Values.Select(x => new PoeEyeConfigMetadata(x)).ToList().ForEach(x => metaConfig.Add(x));

            SaveInternal(metaConfig);
        }

        public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
        {
            if (loadedConfigs.IsEmpty)
            {
                Reload();
            }
            return (TConfig)loadedConfigs.GetOrAdd(typeof(TConfig).FullName, (key) => (TConfig)Activator.CreateInstance(typeof(TConfig)));
        }

        private IPoeEyeConfig ValidateConfigVersion(IPoeEyeConfig loadedConfig)
        {
            var versionedLoadedConfig = loadedConfig as IPoeEyeConfigVersioned;
            Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.ValidateConfigVersion] Validating config of type {loadedConfig} (version(-1 = unversioned): {versionedLoadedConfig?.Version ?? -1})...");
            if (versionedLoadedConfig == null)
            {
                return loadedConfig;
            }

            var configTemplate = (IPoeEyeConfigVersioned)loadedConfigs.GetOrAdd(
                loadedConfig.GetType().FullName, 
                (key) => (IPoeEyeConfigVersioned)Activator.CreateInstance(loadedConfig.GetType()));
            
            if (configTemplate.Version != versionedLoadedConfig.Version)
            {
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.ValidateConfigVersion] Config version mismatch (expected: {configTemplate.Version}, got: {versionedLoadedConfig.Version})");
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.ValidateConfigVersion] Loaded config:\n{loadedConfig.DumpToText()}\n\nTemplate config:\n{configTemplate.DumpToText()}");
                return configTemplate;
            }
            return loadedConfig;
        }

        private void SaveInternal(PoeEyeCombinedConfig config)
        {
            Guard.ArgumentNotNull(config, nameof(config));

            try
            {
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Save] Serializing config data...");
                var serializedData = JsonConvert.SerializeObject(config, jsonSerializerSettings);

                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Save] Successfully serialized config, got {serializedData.Length} chars");

                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Save] Saving config to file '{configFilePath}'...");

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
                Log.Instance.Warn(
                    $"[PoeEyeConfigProviderFromFile.Save] Exception occurred, config was not saved correctly",
                    ex);
            }
        }

        private PoeEyeCombinedConfig LoadInternal()
        {
            Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Loading config from file '{configFilePath}'...");
            loadedConfigs.Clear();

            if (!File.Exists(configFilePath))
            {
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] File not found, fileName: '{configFilePath}'");
                return new PoeEyeCombinedConfig();
            }

            PoeEyeCombinedConfig result = null;
            try
            {
                var fileData = File.ReadAllText(configFilePath);
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Successfully read {fileData.Length} chars, deserializing...");

                result = JsonConvert.DeserializeObject<PoeEyeCombinedConfig>(fileData, jsonSerializerSettings);
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Successfully deserialized config data");

                if (result == null)
                {
                    Log.Instance.Warn($"[PoeEyeConfigProviderFromFile.Load] Failed to deserialize config\nData:\n{fileData}");
                    throw new ApplicationException("Failed to deserialize existing config");
                }
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[PoeEyeConfigProviderFromFile.Load] Could not deserialize config data, default config will be used", ex);
                CreateBackupOfConfig();
            }
            return result ?? new PoeEyeCombinedConfig();
        }

        public void RegisterConverter(JsonConverter converter)
        {
            Guard.ArgumentNotNull(converter, nameof(converter));

            converters.Add(converter);
        }

        private void CreateBackupOfConfig()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    return;
                }
                
                var backupFileName = Path.Combine(Path.GetDirectoryName(configFilePath), $"{Path.GetFileNameWithoutExtension(configFilePath)}.bak{Path.GetExtension(configFilePath)}");
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Creating a backup of existing config data '{configFilePath}' to '{backupFileName}'");
                File.Copy(configFilePath, backupFileName);
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[PoeEyeConfigProviderFromFile.CreateBackupOfConfig] Failed to create a backup", ex);
            }
        }
        
        private void ReinitializeSerializerSettings()
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple,
                Error = HandleSerializerError
            };

            converters.ToList().ForEach(jsonSerializerSettings.Converters.Add);
        }
        
        private void HandleSerializerError(object sender, ErrorEventArgs args)
        {
            if (sender == null || args == null)
            {
                return;
            }
            //FIXME Serializer errors should be treated appropriately, e.g. load value from default config on error
            Log.Instance.Warn($"[PoeEyeConfigProviderFromFile.SerializerError] Suppresing serializer error ! Path: {args.ErrorContext.Path}, Member: {args.ErrorContext.Member}, Handled: {args.ErrorContext.Handled}", args.ErrorContext.Error);
            args.ErrorContext.Handled = true;
        }

        private sealed class PoeEyeCombinedConfig
        {
            private readonly ICollection<PoeEyeConfigMetadata> items = new List<PoeEyeConfigMetadata>();

            public int Version { get; set; } = 1;

            public IEnumerable<PoeEyeConfigMetadata> Items
            {
                [NotNull]
                get { return items; }
            }

            public PoeEyeCombinedConfig Add([NotNull] PoeEyeConfigMetadata item)
            {
                Guard.ArgumentNotNull(item, nameof(item));

                items.Add(item);
                return this;
            }
        }

        private sealed class PoeEyeConfigMetadata
        {
            public string ConfigTypeName => Content.GetType().FullName;

            public IPoeEyeConfig Content { get; }

            public PoeEyeConfigMetadata(IPoeEyeConfig content)
            {
                Content = content;
            }
        }
    }
}
