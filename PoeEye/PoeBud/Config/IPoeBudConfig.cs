using PoeShared.Modularity;

namespace PoeBud.Config
{
    using System;
    using System.Collections.Generic;

    using JetBrains.Annotations;

    internal interface IPoeBudConfig : IPoeEyeConfig
    {
        bool HideXpBar { get; }

        string LoginEmail { get; }

        string SessionId { get; }

        string CharacterName { get; }

        string UiOverlayName { get; }

        int ExpectedSetsCount { get; }

        TimeSpan StashUpdatePeriod { get; }

        TimeSpan ForegroundWindowRecheckPeriod { get; }

        TimeSpan UserActionDelay { get; }

        ISet<int> StashesToProcess {[NotNull] get; }
    }
}