using System.Windows;
using PoeShared.Converters;
using PoeShared.Native;

namespace PoeBud.Config
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public sealed class PoeBudConfig : IPoeBudConfig, IOverlayConfig
    {
        public bool HideXpBar { get; set; } = false;

        public bool IsEnabled { get; set; } = true;

        public bool HighlightSolution { get; set; } = false;

        [JsonConverter(typeof(SafeDataConverter))]
        public string LoginEmail { get; set; }

        [JsonConverter(typeof(SafeDataConverter))]
        public string SessionId { get; set; }

        public string LeagueId { get; set; }

        public string UiOverlayName { get; set; }

        public int ExpectedSetsCount { get; set; }
        
        public int MaxSlotsPerSolution { get; set; } = 48;

        public TimeSpan StashUpdatePeriod { get; set; } = TimeSpan.FromSeconds(300);

        public TimeSpan ForegroundWindowRecheckPeriod { get; } = TimeSpan.FromMilliseconds(100);

        public TimeSpan UserActionDelay { get; } = TimeSpan.FromMilliseconds(100);

        public string[] StashesToProcess { get; set; } = new string[0];

        public string GetChaosSetHotkey { get; set; } = "None";
        
        public Point OverlayLocation { get; set; }
        
        public Size OverlaySize { get; set; }

        public float OverlayOpacity { get; set; } = 0.75f;

        public int Version { get; set; } = 3;
    }
}