using System;
using System.Linq;
using Newtonsoft.Json;
using PoeShared.Converters;
using PoeShared.Modularity;

namespace PoeEye.Config
{
    internal sealed class PoeEyeUpdateSettingsConfig : IPoeEyeConfigVersioned
    {
        public static readonly TimeSpan DefaultAutoUpdateTimeout = TimeSpan.FromMinutes(30);

        public static readonly UpdateSourceInfo[] WellKnownUpdateSources = new[]
        {
            new UpdateSourceInfo() { Uri = @"http://poeeye.dyndns.biz:9997/files/", Description = "[Stable] DynDns Mirror"} , 
            new UpdateSourceInfo() { Uri = @"http://poeeye.dyndns.biz:9997/alpha/", Description = "[Alpha] DynDns Mirror", RequiresAuthentication = true } , 
        };
        
        public int Version { get; set; } = 7;

        public TimeSpan AutoUpdateTimeout { get; set; } = DefaultAutoUpdateTimeout;
        
        public UpdateSourceInfo UpdateSource { get; set; } = WellKnownUpdateSources.First();
    }

    internal struct UpdateSourceInfo
    {
        [JsonConverter(typeof(SafeDataConverter))]
        public string Uri { get; set; }
        
        public string Description { get; set; }
        
        public bool RequiresAuthentication { get; set; }
        
        [JsonConverter(typeof(SafeDataConverter))]
        public string Username { get; set; }
        
        [JsonConverter(typeof(SafeDataConverter))]
        public string Password { get; set; }

        public override string ToString()
        {
            return $"{nameof(Uri)}: {Uri}, {nameof(Description)}: {Description}";
        }
    }
}