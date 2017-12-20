using System;
using PoeShared.Modularity;

namespace PoeEye.Config
{
    internal sealed class PoeEyeUpdateSettingsConfig : IPoeEyeConfigVersioned
    {
        public int Version { get; set; } = 6;

        public TimeSpan AutoUpdateTimeout { get; set; } = TimeSpan.FromMinutes(30);

        public string UpdateUri { get; set; } = @"http://poeeye.dyndns.biz:9997/files/";
    }
}