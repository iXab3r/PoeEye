using PoeBud.Models;
using PoeShared.Modularity;

namespace PoeBud.Config
{
    using System;
    using System.Collections.Generic;

    using JetBrains.Annotations;

    public interface IPoeBudConfig : IPoeEyeConfigVersioned, IStashUpdaterParameters
    {
        bool HideXpBar { get; }

        string UiOverlayName { get; }

        int ExpectedSetsCount { get; }

        TimeSpan StashUpdatePeriod { get; }

        TimeSpan ForegroundWindowRecheckPeriod { get; }

        TimeSpan UserActionDelay { get; }
    }
}