using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using DynamicData;
using JetBrains.Annotations;
using PoeShared.Services;

namespace PoeShared.Modularity;

public sealed class ConfigProviderFromFile : DisposableReactiveObject, IConfigProviderFromFile
{
    private static readonly IFluentLog Log = typeof(ConfigProviderFromFile).PrepareLogger();

    private static readonly string ConfigFileName = @"config.cfg";

    private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
    private readonly IConfigSerializer configSerializer;

    private readonly SourceCache<IPoeEyeConfig, string> loadedConfigsByType = new(ConfigProviderUtils.GetConfigName);
    private readonly SourceList<IConfigProviderStrategy> strategies = new();
    private readonly NamedLock fileLock = new("ConfigLock");
    private string loadedConfigurationFile;

    public ConfigProviderFromFile(
        IConfigSerializer configSerializer,
        IAppArguments appArguments)
    {
        Log.Info($"Initializing config provider, profile: {appArguments.Profile}");
        MigrateLegacyConfig(appArguments);
        this.configSerializer = configSerializer;
        var candidates = new[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                appArguments.RoamingAppDataDirectory
            }
            .Select(x => Path.Combine(x, appArguments.Profile, ConfigFileName))
            .Select(x => new {Path = x, Exists = File.Exists(x)})
            .ToArray();
        Log.Info($"Configuration matrix, configuration file name: {ConfigFileName}:\n\t{candidates.DumpToTable()}");
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
    }
    
    public string ConfigFilePath { [NotNull] get; }

    public IObservableCache<IPoeEyeConfig, string> Configs => loadedConfigsByType;

    public IObservable<Unit> ConfigHasChanged => configHasChanged;

    public IDisposable RegisterStrategy(IConfigProviderStrategy strategy)
    {
        Log.Debug($"Registering strategy {strategy}, existing strategies: {strategies.Items.DumpToString()}");
        strategies.Insert(0, strategy);
        return Disposable.Create(() => strategies.Remove(strategy));
    }

    public void Reload()
    {
        using (fileLock.Enter())
        {
            Log.Debug($"Reloading configuration...");
            var config = LoadInternal();

            config.Items
                .ToList()
                .ForEach(x =>
                {
                    loadedConfigsByType.AddOrUpdate(x);
                });
        }

        Log.Debug($"Sending notification about config update");
        configHasChanged.OnNext(Unit.Default);
    }

    public void Save(IPoeEyeConfig config)
    {
        using var @lock = fileLock.Enter();
        loadedConfigsByType.AddOrUpdate(config);
        Save();
    }

    public void SaveToFile(FileInfo file, IReadOnlyList<IPoeEyeConfig> configs)
    {
        using var @lock = fileLock.Enter();

        var metaConfig = new PoeEyeCombinedConfig();
        configs.ForEach(x => metaConfig.Add(x));
        Log.Debug($"Saving all configs to {file}, metadata: {new {MetadataVersion = metaConfig.Version, Items = metaConfig.Items.Select(x => new {Type = x.GetType()}).ToArray()}.ToString().TakeChars(500)}");

        SaveInternal(configSerializer, strategies.Items, file.FullName, metaConfig);
    }
    
    public void SaveToFile(FileInfo file)
    {
        var configs = loadedConfigsByType.Items.ToList();
        SaveToFile(file, configs);
    }

    public void Save()
    {
        var targetFile = new FileInfo(ConfigFilePath);
        using (fileLock.Enter())
        {
            Log.Debug($"Saving config to {targetFile}");
            SaveToFile(targetFile);
        }

        Log.Debug($"Saved config to {targetFile}, sending notification about config update");
        configHasChanged.OnNext(Unit.Default);
    }

    public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
    {
        if (typeof(PoeConfigMetadata).IsAssignableFrom(typeof(TConfig)))
        {
            throw new ArgumentException($"Provided config type {typeof(TConfig)} is of metadata type instead of an actual config");
        }
        
        using var @lock = fileLock.Enter();

        if (loadedConfigurationFile == null)
        {
            Log.Info($"Forcing to load initial configuration from file");
            Reload();
        }

        var config = loadedConfigsByType.GetOrAdd(ConfigProviderUtils.GetConfigName(typeof(TConfig)), key => (TConfig) Activator.CreateInstance(typeof(TConfig)));

        if (config is PoeConfigMetadata metadata)
        {
            Log.Debug($"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
            var serialized = configSerializer.Serialize(metadata);
            if (string.IsNullOrEmpty(serialized))
            {
                throw new ApplicationException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
            }

            var deserialized = configSerializer.Deserialize<TConfig>(serialized);
            loadedConfigsByType.AddOrUpdate(deserialized);
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
            Log.Debug($"Saving config to file '{configFilePath}'");
            Log.Debug($"Serializing config data...");
            var serializedData = configSerializer.Serialize(config);

            Log.Debug($"Successfully serialized config, got {serializedData.Length} chars");

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

            Log.Debug($"Preparing temporary file '{temporaryFile}'...");
            if (temporaryFile.Exists)
            {
                Log.Debug($"Removing previous temporary file '{temporaryFile}'...");
                temporaryFile.Delete();
            }

            Log.Debug($"Writing configuration({serializedData.Length}) to temporary file '{temporaryFile}'...");
            File.WriteAllText(temporaryConfigPath, serializedData, Encoding.Unicode);
            temporaryFile.Refresh();
            Log.Debug($"Flushing configuration '{temporaryConfigPath}' => '{configFilePath}'");

            if (backupFile.Exists)
            {
                Log.Debug($"Removing previous backup file {backupFile}");
                backupFile.Delete();
            }

            if (configFile.Exists)
            {
                Log.Debug($"Moving previous config to backup {configFile.FullName} => {backupFile.FullName}");
                configFile.MoveTo(backupConfigPath);
            }

            Log.Debug($"Moving temporary config to default {temporaryFile.FullName} => {configFile.FullName}");
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
        Log.Debug($"Loading config from file '{ConfigFilePath}'");
        loadedConfigsByType.Clear();
        loadedConfigurationFile = string.Empty;

        if (!File.Exists(ConfigFilePath))
        {
            Log.Debug($"File not found, fileName: '{ConfigFilePath}'");
            return new PoeEyeCombinedConfig();
        }

        PoeEyeCombinedConfig result = null;
        Exception thrownException = null;
        try
        {
            var fileData = File.ReadAllText(ConfigFilePath);
            Log.Debug($"Successfully read {fileData.Length} chars, deserializing...");
            loadedConfigurationFile = fileData;

            result = configSerializer.Deserialize<PoeEyeCombinedConfig>(fileData);
            Log.Debug($"Successfully deserialized config data");
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not deserialize config data", ex);
            thrownException = ex;

            foreach (var strategy in strategies.Items)
            {
                if (strategy.TryHandleConfigLoadException(new FileInfo(ConfigFilePath), out var strategyResult) && strategyResult != null)
                {
                    Log.Debug($"Strategy {strategy} has handled exception");
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
            throw new ApplicationException($"Could not load configuration from {ConfigFilePath}", thrownException);
        }

        return result;
    }

    private static void MigrateLegacyConfig(IAppArguments appArguments)
    {
        const string DebugConfigFileName = @"configDebugMode.cfg";
        const string ReleaseConfigFileName = @"config.cfg";

        if (string.IsNullOrEmpty(appArguments.Profile))
        {
            Log.Debug($"Profile is not defined - skipping migration");
            return;
        }

        Log.Debug($"Checking if legacy config file migration is needed for profile {appArguments.Profile}");
        var configName = appArguments.Profile.ToLower() switch
        {
            "debug" => DebugConfigFileName,
            "release" => ReleaseConfigFileName,
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(configName))
        {
            Log.Info($"Non-legacy profile {appArguments.Profile} - skipping migration");
            return;
        }

        var configFile = new FileInfo(Path.Combine(appArguments.RoamingAppDataDirectory, configName));
        if (!configFile.Exists)
        {
            Log.Info($"Legacy config file {configFile.FullName} does not exist");
            return;
        }
        
        var targetConfigFilePathBackup = Path.ChangeExtension(configFile.FullName, "bak");
        if (File.Exists(targetConfigFilePathBackup))
        {
            Log.Info($"Cleaning up existing legacy backup config @ {targetConfigFilePathBackup}");
            File.Delete(targetConfigFilePathBackup);
        }

        var targetConfigFilePath = Path.Combine(appArguments.AppDataDirectory, ReleaseConfigFileName);
        Log.Info($"Moving legacy config file {configFile.FullName} => {targetConfigFilePath}");
        if (!Directory.Exists(appArguments.AppDataDirectory))
        {
            Log.Info($"Creating directory {appArguments.AppDataDirectory}");
            Directory.CreateDirectory(appArguments.AppDataDirectory);
        }

        if (File.Exists(targetConfigFilePath))
        {
            Log.Info($"Cleaning up existing non-legacy config @ {targetConfigFilePath}");
            File.Delete(targetConfigFilePath);
        }
        configFile.MoveTo(targetConfigFilePath);

       
        
        Log.Info($"Migration is completed");
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