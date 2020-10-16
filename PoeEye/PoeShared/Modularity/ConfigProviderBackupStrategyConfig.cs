using System;

namespace PoeShared.Modularity
{
    public sealed class ConfigProviderBackupStrategyConfig : IPoeEyeConfigVersioned
    {
        public string BackupDirectoryName { get; set; } = "backups";

        /// <summary>
        ///   Get/Set number of backup files to keep
        ///   E.g. 5 = backup folder will have at least 5 backups even if they are outdated
        /// </summary>
        public uint BackupStorageMinSize { get; set; } = 10;

        /// <summary>
        ///   Get/Set Time period which will be covered by backups.
        ///   E.g. 6h = all backups older than 6h will be removed on each cycle
        /// </summary>
        public TimeSpan BackupStoragePeriod { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        ///   Get/Set Time period between backups
        ///   E.g. 15m = backup will be made if only if more than 15m elapsed since last one
        /// </summary>
        public TimeSpan BackupTimeout { get; set; } = TimeSpan.FromMinutes(30);
        
        public int Version { get; set; } = 1;
    }
}