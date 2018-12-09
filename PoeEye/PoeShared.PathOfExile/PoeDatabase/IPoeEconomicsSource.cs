using System.Collections.Generic;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeDatabase
{
    public interface IPoeEconomicsSource
    {
        [NotNull]
        IEnumerable<PoePrice> GetCurrenciesInChaosEquivalent([NotNull] string leagueId);
    }
}