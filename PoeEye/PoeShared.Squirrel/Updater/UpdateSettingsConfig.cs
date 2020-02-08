using System;
using System.Linq;
using PoeShared.Modularity;

namespace PoeShared.Squirrel.Updater
{
    public sealed class UpdateSettingsConfig : IPoeEyeConfigVersioned
    {
        public static readonly TimeSpan DefaultAutoUpdateTimeout = TimeSpan.FromMinutes(30);

        //FIXME Remove EyeAuras update source !
        public static readonly UpdateSourceInfo[] WellKnownUpdateSources =
        {
            new UpdateSourceInfo {Uri = @"https://github.com/iXab3r/EyeAuras", Description = "GitHub"}
        };

        public TimeSpan AutoUpdateTimeout { get; set; } = DefaultAutoUpdateTimeout;

        public UpdateSourceInfo UpdateSource { get; set; } = WellKnownUpdateSources.First();

        public int Version { get; set; } = 9;
    }
}