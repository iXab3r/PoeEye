using PoeShared.Modularity;

namespace PoeBud.Config
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public sealed class PoeBudConfig : IPoeBudConfig
    {
        public bool HideXpBar { get; set; } = false;

        public bool IsEnabled { get; set; } = true;

        [JsonConverter(typeof(SafeDataConverter))]
        public string LoginEmail { get; set; }

        [JsonConverter(typeof(SafeDataConverter))]
        public string SessionId { get; set; }

        public string CharacterName { get; set; }

        public string UiOverlayName { get; set; }

        public int ExpectedSetsCount { get; set; }

        public TimeSpan StashUpdatePeriod { get; set; } = TimeSpan.FromSeconds(300);

        public TimeSpan ForegroundWindowRecheckPeriod { get; } = TimeSpan.FromMilliseconds(100);

        public TimeSpan UserActionDelay { get; } = TimeSpan.FromMilliseconds(100);

        public ICollection<int> StashesToProcess { get; set; } = new HashSet<int>();

        public string GetChaosSetHotkey { get; set; } = "None";
    }
}