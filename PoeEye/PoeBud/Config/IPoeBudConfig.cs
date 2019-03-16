using System;
using PoeBud.Models;
using PoeShared.Modularity;
using PoeShared.Native;

namespace PoeBud.Config
{
    public interface IPoeBudConfig : IPoeEyeConfigVersioned, IStashUpdaterParameters, ICanBeEnabled
    {
        int ExpectedSetsCount { get; }

        int MaxSlotsPerSolution { get; }

        TimeSpan StashUpdatePeriod { get; }

        TimeSpan ForegroundWindowRecheckPeriod { get; }

        TimeSpan UserActionDelay { get; }
    }
}