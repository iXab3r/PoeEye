using System;
using PoeBud.Models;
using PoeShared.Modularity;

namespace PoeBud.Config
{
    public interface IPoeBudConfig : IPoeEyeConfigVersioned, IStashUpdaterParameters
    {
        int ExpectedSetsCount { get; }

        int MaxSlotsPerSolution { get; }

        TimeSpan StashUpdatePeriod { get; }

        TimeSpan ForegroundWindowRecheckPeriod { get; }

        TimeSpan UserActionDelay { get; }
    }
}