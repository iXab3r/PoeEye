using System;
using PoeShared.Modularity;

namespace PoeEye.Config
{
    internal sealed class PoeEyeUpdateSettingsConfig : IPoeEyeConfigVersioned
    {
        public int Version { get; set; } = 5;

        public TimeSpan AutoUpdateTimeout { get; set; } = TimeSpan.FromMinutes(0);

        public string UpdateUri { get; set; } = @"http://coderush.net/files/PoeEye/";
    }
}