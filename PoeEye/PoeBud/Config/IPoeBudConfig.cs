using PoeBud.Models;
using PoeShared.Modularity;

namespace PoeBud.Config
{
    using System;
    using System.Collections.Generic;

    using JetBrains.Annotations;

    public interface IPoeBudConfig : IPoeEyeConfigVersioned, IStashUpdaterParameters
    {
        int ExpectedSetsCount { get; }
        
        int MaxSlotsPerSolution { get; }

        TimeSpan StashUpdatePeriod { get; }

        TimeSpan ForegroundWindowRecheckPeriod { get; }

        TimeSpan UserActionDelay { get; }
    }
}