using System.Collections.Generic;
using JetBrains.Annotations;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    public interface IStashUpdaterStrategy
    {
        [NotNull]
        IStashTab[] GetTabsToProcess([NotNull] IEnumerable<IStashTab> tabs);

        [NotNull]
        ILeague[] GetLeaguesToProcess([NotNull] IEnumerable<ILeague> leagues);

        [NotNull] 
        ILeague[] GetDefaultLeaguesList();
    }
}