using System;
using System.IO;
using System.Linq;
using PoeShared.Logging;
using PoeShared.Scaffolding; 

namespace PoeShared.Modularity;

public sealed class ConfigProviderBackupStrategy : DisposableReactiveObject, IConfigProviderStrategy
{
    private static readonly IFluentLog Log = typeof(ConfigProviderBackupStrategy).PrepareLogger();

    private readonly IClock clock;
    private DateTime lastBackup;
 

    public ConfigProviderBackupStrategy(
        IClock clock)
    {
        this.clock = clock;
        Properties = new ConfigProviderBackupStrategyConfig();
    }

    public ConfigProviderBackupStrategyConfig Properties { get; set; }
        
    public void HandleConfigSave(FileInfo configFile)
    {
        if (Properties == null)
        {
            Log.Warn($"Properties are not set");
            return;
        }
        if (Properties.BackupTimeout <= TimeSpan.Zero)
        {
            Log.Warn($"Backup timeout is not set");
            return;
        }
        if (string.IsNullOrEmpty(Properties.BackupDirectoryName))
        {
            Log.Warn($"Backup directory is not set");
            return;
        }
        if (!configFile.Exists)
        {
            Log.Warn($"Config does not exist: {configFile}");
            return;
        }

        var configFileName = configFile.Name;
        var configFileDirectory = configFile.DirectoryName;
        if (string.IsNullOrEmpty(configFileDirectory))
        {
            Log.Warn($"Failed to get config file directory, config: {configFile}");
            return;
        }

        var now = clock.Now;
        var timeSinceBackup = now - lastBackup;
        Log.Debug(() => $"Configuration file saved, last backup was made {timeSinceBackup} ago");

        if (timeSinceBackup < Properties.BackupTimeout)
        {
            Log.Debug(() => $"Backup timeout has not passed yet: {timeSinceBackup} < {Properties.BackupTimeout}");
            return;
        }

        var timestampedBackupDirectory = now.ToString(@"yyyy-MM-dd HHmmss");
        var backupsStorageDirectoryPath = Path.Combine(configFileDirectory, Properties.BackupDirectoryName);
        var backupDirectory = new DirectoryInfo(Path.Combine(backupsStorageDirectoryPath, timestampedBackupDirectory));
        var backupConfigPath = Path.Combine(backupDirectory.FullName, configFileName);

        try
        {
            if (backupDirectory.Exists)
            {
                Log.Debug(() => $"Backup directory {backupDirectory} already exists, removing it");
                backupDirectory.Delete(true);
            }
            Log.Debug(() => $"Creating backup directory {backupDirectory}");
            backupDirectory.Create();

            Log.Debug(() => $"Copying existing configuration to backup, {configFile.FullName} => {backupConfigPath}");
            configFile.CopyTo(backupConfigPath);
            lastBackup = now;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to perform config backup, config file: {configFile.FullName}, backup: {backupConfigPath}", e);
        }

        try
        {
            CleanupStorage(new DirectoryInfo(backupsStorageDirectoryPath), clock.Now, Properties.BackupStorageMinSize, Properties.BackupStoragePeriod);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to perform backup storage cleanup", e);
        }
    }

    public bool TryHandleConfigLoadException(FileInfo configFile, out ConfigProviderFromFile.PoeEyeCombinedConfig replacementConfig)
    {
        Log.Debug(() => $"Creating backup of config {configFile.FullName}");
        CreateBackupOfConfig(configFile, clock.Now);
        replacementConfig = null;
        return false;
    }

    private static void CleanupStorage(
        DirectoryInfo backupDirectory, 
        DateTime now,
        uint backupStorageMinSize,
        TimeSpan backupStoragePeriod)
    {
        if (backupStoragePeriod <= TimeSpan.Zero)
        {
            Log.Warn($"Backup cleanup period is not set");
            return;
        }

        var possibleOldestBackupTimestamp = now - backupStoragePeriod;
        Log.Debug(() => $"Cleaning up backup storage, period: {backupStoragePeriod} (LastWriteTime <= {possibleOldestBackupTimestamp})");
        var obsoleteBackups = backupDirectory
            .EnumerateFiles("*.*", SearchOption.AllDirectories)
            .Where(x => x.LastWriteTime < possibleOldestBackupTimestamp)
            .OrderByDescending(x => x.LastWriteTime)
            .ToArray();
        Log.Debug(() => $"Backup storage contains {obsoleteBackups.Length} obsolete backups, min storage size: {backupStorageMinSize}");
        obsoleteBackups = obsoleteBackups.Skip((int)backupStorageMinSize).ToArray();
        Log.Debug(() => $"Cleaning up {obsoleteBackups.Length} obsolete backups:\r\n\t{obsoleteBackups.Select(x => x.FullName).DumpToString()}");
        foreach (var obsoleteFile in obsoleteBackups)
        {
            Log.Debug(() => $"Removing file {obsoleteFile}");
            obsoleteFile.Delete();
            if (obsoleteFile.Directory == null || !obsoleteFile.Directory.Exists)
            {
                continue;
            }
            obsoleteFile.Directory.Refresh();
            if (obsoleteFile.Directory.GetFiles().Any())
            {
                Log.Debug(() => $"Directory {obsoleteFile.Directory} still has files, skipping it");
                continue;
            }
            Log.Debug(() => $"Removing empty directory {obsoleteFile.Directory}");
            obsoleteFile.Directory.Delete(true);
        }
    }
        
    private static void CreateBackupOfConfig(FileInfo configFile, DateTime timestamp)
    {
        try
        {
            if (!configFile.Exists)
            {
                return;
            }

            var configFileName = Path.GetFileNameWithoutExtension(configFile.FullName);
            if (string.IsNullOrEmpty(configFileName))
            {
                throw new ApplicationException($"Invalid config file path: {configFile}");
            }

            var corruptedFileName = $"{configFileName}.corrupted.{timestamp:yyyyMMddHHmmss}{configFile.Extension}";
            var backupFilePath = Path.Combine(configFile.DirectoryName!, corruptedFileName);
                
            Log.Debug(() => $"Creating a backup of existing config data '{configFile}' to '{backupFilePath}'");
            configFile.CopyTo(backupFilePath, true);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to create a backup", ex);
        }
    }
}