using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using PoeShared;
using PoeShared.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    internal sealed class UpdaterDefaultProcessAllStrategy : IDefaultStashUpdaterStrategy
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(UpdaterDefaultProcessAllStrategy));

        private readonly IClock clock;
        private readonly IStashUpdaterParameters parameters;
        private ILeague[] leaguesToProcess;

        public UpdaterDefaultProcessAllStrategy(IClock clock, IStashUpdaterParameters parameters)
        {
            this.clock = clock;
            this.parameters = parameters;
            Log.Debug($"[League {parameters.LeagueId}] Strategy is to process the following tabs: {parameters.StashesToProcess.DumpToTextRaw()}");
        }

        public IStashTab[] GetTabsToProcess(IEnumerable<IStashTab> tabs)
        {
            var publicTabs = tabs
                             .EmptyIfNull()
                             .Where(x => !x.Hidden)
                             .Where(x => !parameters.StashesToProcess.Any() || parameters.StashesToProcess.Contains(x.Name))
                             .ToArray();
            Log.Debug($"Public tabs to process: {publicTabs.Select(x => x.Name).DumpToTextRaw()}");
            return publicTabs;
        }

        public ILeague[] GetLeaguesToProcess(IEnumerable<ILeague> leagues)
        {
            leaguesToProcess = leagues
                               .Where(x => x.StartAt <= clock.Now && x.EndAt >= clock.Now)
                               .Where(x => parameters.LeagueId == x.Id)
                               .ToArray();
            Log.Debug($"Leagues to process: {leaguesToProcess.Select(x => x.Id).DumpToTextRaw()}");
            return leaguesToProcess.ToArray();
        }

        public ILeague[] GetDefaultLeaguesList()
        {
            return leaguesToProcess ?? new ILeague[0];
        }

        public override string ToString()
        {
            return $"[UpdaterDefaultProcessAllStrategy] League{parameters.LeagueId}";
        }
    }
}