using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Scaffolding; 

namespace PoeShared.Modularity
{
    public sealed class ConfigProviderFromFile : DisposableReactiveObject, IConfigProvider
    {
        private static readonly IFluentLog Log = typeof(ConfigProviderFromFile).PrepareLogger();

        private static readonly string DebugConfigFileName = @"configDebugMode.cfg";
        private static readonly string ReleaseConfigFileName = @"config.cfg";

        private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
        private readonly IConfigSerializer configSerializer;

        private readonly ConcurrentDictionary<string, IPoeEyeConfig> loadedConfigsByType = new ConcurrentDictionary<string, IPoeEyeConfig>();
        private readonly SourceList<IConfigProviderStrategy> strategies = new SourceList<IConfigProviderStrategy>();
        private string loadedConfigurationFile;

        public ConfigProviderFromFile(
            IConfigSerializer configSerializer,
            IAppArguments appArguments)
        {
            this.configSerializer = configSerializer;

            string configFileName;
            if (appArguments.IsDebugMode)
            {
                Log.Info($"Debug mode detected");
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
                .Select(x => new {Path = x, Exists = File.Exists(x)})
                .ToArray();
            Log.Debug(() => $"Configuration matrix, configuration file name: {configFileName}:\n\t{candidates.DumpToString()}");
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

        public IDisposable RegisterStrategy(IConfigProviderStrategy strategy)
        {
            Log.Debug(() => $"Registering strategy {strategy}, existing strategies: {strategies.Items.DumpToString()}");
            strategies.Insert(0, strategy);
            return Disposable.Create(() => strategies.Remove(strategy));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Reload()
        {
            Log.Debug(() => $"Reloading configuration...");

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
        public void SaveToFile(FileInfo file)
        {
            var metaConfig = new PoeEyeCombinedConfig();
            loadedConfigsByType.Values.ToList().ForEach(x => metaConfig.Add(x));
            Log.Debug(() => $"Saving all configs, metadata: {metaConfig.DumpToTextRaw().TakeChars(500)}");

            SaveInternal(configSerializer, strategies.Items, file.FullName, metaConfig);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Save()
        {
            SaveToFile(new FileInfo(ConfigFilePath));
            configHasChanged.OnNext(Unit.Default);
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
                Log.Debug(() => $"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
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

        private static void SaveInternal(
            IConfigSerializer configSerializer,
            IEnumerable<IConfigProviderStrategy> strategies,
            string configFilePath, 
            PoeEyeCombinedConfig config)
        {
            try
            {
                Log.Debug(() => $"Saving config to file '{configFilePath}'");
                Log.Debug(() => $"Serializing config data...");
                var serializedData = configSerializer.Serialize(config);

                Log.Debug(() => $"Successfully serialized config, got {serializedData.Length} chars");

                var directoryPath = Path.GetDirectoryName(configFilePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                var temporaryConfigPath = Path.ChangeExtension(configFilePath, ".new");
                var backupConfigPath = Path.ChangeExtension(configFilePath, ".bak");
                if (string.IsNullOrEmpty(temporaryConfigPath) || string.IsNullOrEmpty(backupConfigPath))
                {
                    throw new ApplicationException($"Failed to prepare path for a temporary config file, file path: {configFilePath}");
                }
                var configFile = new FileInfo(configFilePath);
                var temporaryFile = new FileInfo(temporaryConfigPath);
                var backupFile = new FileInfo(backupConfigPath);

                Log.Debug(() => $"Preparing temporary file '{temporaryFile}'...");
                if (temporaryFile.Exists)
                {
                    Log.Debug(() => $"Removing previous temporary file '{temporaryFile}'...");
                    temporaryFile.Delete();
                }

                Log.Debug(() => $"Writing configuration({serializedData.Length}) to temporary file '{temporaryFile}'...");
                File.WriteAllText(temporaryConfigPath, serializedData, Encoding.Unicode);
                temporaryFile.Refresh();
                Log.Debug(() => $"Flushing configuration '{temporaryConfigPath}' => '{configFilePath}'");

                if (backupFile.Exists)
                {
                    Log.Debug(() => $"Removing previous backup file {backupFile}");
                    backupFile.Delete();
                }

                if (configFile.Exists)
                {
                    Log.Debug(() => $"Moving previous config to backup {configFile.FullName} => {backupFile.FullName}");
                    configFile.MoveTo(backupConfigPath);   
                }
                
                Log.Debug(() => $"Moving temporary config to default {temporaryFile.FullName} => {configFile.FullName}");
                temporaryFile.MoveTo(configFilePath);
                
                strategies.ForEach(x => x.HandleConfigSave(new FileInfo(configFilePath)));
            }
            catch (Exception ex)
            {
                Log.Warn($"Exception occurred, config was not saved correctly", ex);
            }
        }

        private PoeEyeCombinedConfig LoadInternal()
        {
            Log.Debug(() => $"Loading config from file '{ConfigFilePath}'");
            loadedConfigsByType.Clear();
            loadedConfigurationFile = string.Empty;

            if (!File.Exists(ConfigFilePath))
            {
                Log.Debug(() => $"File not found, fileName: '{ConfigFilePath}'");
                return new PoeEyeCombinedConfig();
            }

            PoeEyeCombinedConfig result = null;
            try
            {
                var fileData = File.ReadAllText(ConfigFilePath);
                Log.Debug(() => $"Successfully read {fileData.Length} chars, deserializing...");
                loadedConfigurationFile = fileData;

                result = configSerializer.Deserialize<PoeEyeCombinedConfig>(fileData);
                Log.Debug(() => $"Successfully deserialized config data");
            }
            catch (Exception ex)
            {
                Log.Warn($"Could not deserialize config data", ex);

                foreach (var strategy in strategies.Items)
                {
                    if (strategy.TryHandleConfigLoadException(new FileInfo(ConfigFilePath), out var strategyResult) && strategyResult != null)
                    {
                        Log.Debug(() => $"Strategy {strategy} has handled exception");
                        result = strategyResult;
                        break;
                    }
                }

                if (result == null)
                {
                    Log.Warn($"Strategies were not able to handle config load exception");
                    throw;
                }
            }

            if (result == null)
            {
                throw new ApplicationException($"Could not load configuration from {ConfigFilePath}");
            }

            return result;
        }


        public sealed class PoeEyeCombinedConfig : IPoeEyeConfigVersioned
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