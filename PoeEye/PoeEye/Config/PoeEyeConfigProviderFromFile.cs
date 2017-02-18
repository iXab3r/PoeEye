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
            var config = LoadInternal();
            loadedConfigs.Clear();

            config.Items
                .ToList()
                .Select(x => x.Content)
                .Select(ValidateConfigVersion)
                .ForEach(x => loadedConfigs[x.GetType().FullName] = x);

            configHasChanged.OnNext(Unit.Default);
        }

        public void Save()
        {
            var config = new PoeEyeCombinedConfig();
            loadedConfigs.Values.Select(x => new PoeEyeConfigMetadata(x)).ToList().ForEach(x => config.Add(x));

            SaveInternal(config);
        }

        private IPoeEyeConfig ValidateConfigVersion(IPoeEyeConfig loadedConfig)
        {
            var versionedLoadedConfig = loadedConfig as IPoeEyeConfigVersioned;
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
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.ValidateConfigVersion] \nLoaded config:\n{loadedConfig.DumpToText()}\n\nTemplate config:\n{configTemplate.DumpToText()}");
                return configTemplate;
            }
            return loadedConfig;
        }

        public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
        {
            if (loadedConfigs.IsEmpty)
            {
                Reload();
            }
            return (TConfig)loadedConfigs.GetOrAdd(typeof(TConfig).FullName, (key) => (TConfig)Activator.CreateInstance(typeof(TConfig)));
        }

        private void SaveInternal(PoeEyeCombinedConfig config)
        {
            Guard.ArgumentNotNull(() => config);

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

                Reload();
            }
            catch (Exception ex)
            {
                Log.Instance.Warn(
                    $"[PoeEyeConfigProviderFromFile.Save] Exception occurred, config was not save correctly",
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

            PoeEyeCombinedConfig result;
            try
            {
                var fileData = File.ReadAllText(configFilePath);
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Successfully read {fileData.Length} chars, deserializing...");

                result = JsonConvert.DeserializeObject<PoeEyeCombinedConfig>(fileData, jsonSerializerSettings);
                Log.Instance.Debug($"[PoeEyeConfigProviderFromFile.Load] Successfully deserialized config data");
            }
            catch (Exception ex)
            {
                Log.Instance.Warn($"[PoeEyeConfigProviderFromFile.Load] Could not deserialize config data, default config will be used", ex);
                result = new PoeEyeCombinedConfig();
            }

            return result;
        }

        public void RegisterConverter(JsonConverter converter)
        {
            Guard.ArgumentNotNull(() => converter);

            converters.Add(converter);
        }

        private void ReinitializeSerializerSettings()
        {
            jsonSerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
            };

            converters.ToList().ForEach(jsonSerializerSettings.Converters.Add);
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
                Guard.ArgumentNotNull(() => item);

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