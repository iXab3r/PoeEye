using System.Reactive;
using System.Reactive.Subjects;
using System.Text;
using DynamicData;
using PoeShared.Services;

namespace PoeShared.Modularity;

public sealed class ConfigProviderFromMultipleFiles : DisposableReactiveObject, IConfigProvider
{
    private static readonly IFluentLog Log = typeof(ConfigProviderFromMultipleFiles).PrepareLogger();
    private readonly IConfigSerializer configSerializer;

    private readonly SourceCache<IPoeEyeConfig, string> loadedConfigsByType = new(ConfigProviderUtils.GetConfigName);
    private readonly ISubject<Unit> configHasChanged = new Subject<Unit>();
    private readonly DirectoryInfo configDirectory;
    private readonly NamedLock fileLock = new("MultiFileConfigLock");

    public ConfigProviderFromMultipleFiles(
        IConfigSerializer configSerializer,
        IAppArguments appArguments)
    {
        Log.Info("Initializing multi-file config provider");

        this.configSerializer = configSerializer;

        var candidates = new[]
            {
                Path.Combine(appArguments.AppDomainDirectory, appArguments.Profile, "config"),
                Path.Combine(appArguments.RoamingAppDataDirectory, appArguments.Profile, "config")
            }
            .Select(x => new DirectoryInfo(x))
            .ToArray();
        Log.Debug($"Configuration matrix, directories:\n\t{candidates.SelectSafe(x => new { x.FullName, x.Exists }).DumpToString()}");
        var existingDirectory = candidates.FirstOrDefault(x => x.Exists);
        if (existingDirectory != null)
        {
            configDirectory = existingDirectory;
            Log.Info($"Using existing configuration directory {configDirectory}");
        }
        else
        {
            configDirectory = candidates.Last();
            Log.Info($"Configuration directory not found, using {configDirectory}");
        }

        if (!configDirectory.Exists)
        {
            Log.Info($"Creating configuration directory: {configDirectory.FullName}");
            Directory.CreateDirectory(configDirectory.FullName);
        }
    }

    public IObservable<Unit> ConfigHasChanged => configHasChanged;
    
    public IObservableCache<IPoeEyeConfig, string> Configs => loadedConfigsByType;

    public void Save()
    {
        SaveToDirectory(configDirectory);
        Log.Debug($"Saved config, sending notification about config update");
        configHasChanged.OnNext(Unit.Default);
    }

    public void SaveToDirectory(DirectoryInfo directory, IReadOnlyList<IPoeEyeConfig> configs)
    {
        using var @lock = fileLock.Enter();

        Log.Info($"Saving configs(total: {configs.Count()})");
        foreach (var config in configs)
        {
            Log.Info($"Saving config of type {config.GetType()}");
            SaveInternal(configSerializer, config, directory);
            Log.Info($"Saved config of type {config.GetType()}");
        }
    }
    
    public void SaveToDirectory(DirectoryInfo directory)
    {
        using var @lock = fileLock.Enter();
        SaveToDirectory(directory, loadedConfigsByType.Items.ToArray());
    }

    public void Save(IPoeEyeConfig config)
    {
        using var @lock = fileLock.Enter();
        
        loadedConfigsByType.AddOrUpdate(config);
        SaveInternal(config);
    }

    public TConfig GetActualConfig<TConfig>() where TConfig : IPoeEyeConfig, new()
    {
        if (typeof(PoeConfigMetadata).IsAssignableFrom(typeof(TConfig)))
        {
            throw new ArgumentException($"Provided config type {typeof(TConfig)} is of metadata type instead of an actual config");
        }
        
        using var @lock = fileLock.Enter();

        var configName = ConfigProviderUtils.GetConfigName(typeof(TConfig));
        if (loadedConfigsByType.TryGetValue(configName, out var existingConfig))
        {
            if (existingConfig is TConfig eyeConfig)
            {
                return eyeConfig;
            }

            Log.Debug($"Config is not of type {typeof(TConfig)}, but of {existingConfig.GetType()}, attempting to deserialize");
            var result = DeserializeIfNeeded<TConfig>(existingConfig);
            loadedConfigsByType.AddOrUpdate(result);
            return GetActualConfig<TConfig>();
        }
        
        Log.Debug($"Config of type {typeof(TConfig)} is not loaded, loading it from storage");
        var config = LoadInternal<TConfig>();
        loadedConfigsByType.AddOrUpdate(config);
        return config;
    }

    private TConfig DeserializeIfNeeded<TConfig>(IPoeEyeConfig config)
    {
        if (config is TConfig)
        {
            return (TConfig) config;
        }
        Log.Warn($"Supplied config is not of expected type, expected: {typeof(TConfig)}, got: {config.GetType()}");
        if (config is not PoeConfigMetadata metadata)
        {
            throw new InvalidStateException($"Expected config of type {typeof(TConfig)}, got: {config.GetType()}");
        }
        Log.Debug($"Trying to re-serialize metadata type {metadata.TypeName} (v{metadata.Version}) {metadata.AssemblyName}...");
        var serialized = configSerializer.Serialize(metadata);
        if (string.IsNullOrEmpty(serialized))
        {
            throw new InvalidStateException($"Something went wrong when re-serializing metadata: {metadata}\n{metadata.ConfigValue}");
        }

        var deserialized = configSerializer.Deserialize<TConfig>(serialized);
        if (deserialized is PoeConfigMetadata)
        {
            Log.Warn($"Failed to deserialize config metadata into {typeof(TConfig)}: {metadata}");
            throw new InvalidStateException($"Failed to deserialize metadata: : {metadata}\n{metadata.ConfigValue}");
        }
        return deserialized;
    }

    private void SaveInternal(IPoeEyeConfig config)
    {
        SaveInternal(configSerializer, config, configDirectory);
    }

    private static void SaveInternal(
        IConfigSerializer configSerializer,
        IPoeEyeConfig config, 
        DirectoryInfo configDirectory)
    {
        var configFilePath = GetConfigFilePath(config, configDirectory);
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
        }
        catch (Exception ex)
        {
            Log.Warn($"Exception occurred, config was not saved correctly", ex);
        }
    }

    private static string GetConfigFilePath(IPoeEyeConfig config, DirectoryInfo configDirectory)
    {
        return GetConfigFilePath(ConfigProviderUtils.GetConfigName(config), configDirectory);
    }
    
    private static string GetConfigFilePath(Type configType, DirectoryInfo configDirectory)
    {
        return GetConfigFilePath(ConfigProviderUtils.GetConfigName(configType), configDirectory);
    }

    private static string GetConfigFilePath(string configName, DirectoryInfo configDirectory)
    {
        var configFilePath = Path.Combine(configDirectory.FullName, $"{configName}.cfg");
        return configFilePath;
    }
    
    private TConfig LoadInternal<TConfig>() where TConfig : new()
    {
        var configFilePath = GetConfigFilePath(typeof(TConfig), configDirectory);
        Log.Debug($"Loading config from file '{configFilePath}'");
        if (!File.Exists(configFilePath))
        {
            Log.Debug($"File not found, fileName: '{configFilePath}'");
            return new TConfig();
        }

        TConfig result = default;
        Exception thrownException = null;
        try
        {
            var fileData = File.ReadAllText(configFilePath);
            Log.Debug($"Successfully read {fileData.Length} chars, deserializing...");

            result = configSerializer.Deserialize<TConfig>(fileData);
            Log.Debug($"Successfully deserialized config data");
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not deserialize config data", ex);
            thrownException = ex;
        }

        if (result == null)
        {
            throw new ApplicationException($"Could not load configuration from {configFilePath}", thrownException);
        }

        return result;
    }
}