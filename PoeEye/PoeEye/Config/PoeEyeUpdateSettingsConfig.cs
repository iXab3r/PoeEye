using System;
using PoeShared.Modularity;

namespace PoeEye.Config
{
    internal sealed class PoeEyeUpdateSettingsConfig : IPoeEyeConfigVersioned
    {
        public static readonly TimeSpan DefaultAutoUpdateTimeout = TimeSpan.FromMinutes(30);
        
        public int Version { get; set; } = 7;

        public TimeSpan AutoUpdateTimeout { get; set; } = DefaultAutoUpdateTimeout;
        
        public string UpdateUri { get; set; } = @"http://poeeye.dyndns.biz:9997/files/";
    }
}