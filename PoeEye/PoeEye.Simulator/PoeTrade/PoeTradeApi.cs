using System;

namespace PoeEye.Simulator.PoeTrade
{
    using System.Linq;
    using System.Threading.Tasks;

    using Guards;

    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    internal sealed class PoeTradeApi : PoeApi
    {
        public override Guid Id { get; } = Guid.Parse("523815CB-99C8-4743-A7D7-0682E5678182");

        public override string Name { get; } = "SimulatorApi";

        public override bool IsAvailable { get; } = true;

        public override Task<IPoeQueryResult> IssueQuery(IPoeQueryInfo query)
        {
            Guard.ArgumentNotNull(query, nameof(query));

            return Task.Run(() => ProcessQuery(query));
        }

        public override Task<IPoeStaticData> RequestStaticData()
        {
            return Task.Run(() => PrepareStatisData());
        }

        private IPoeStaticData PrepareStatisData()
        {
            return new PoeStaticData()
            {
                LeaguesList = new[] {"League 1", "League 2"},
                ModsList = new IPoeItemMod[0],
                CurrenciesList = new IPoeCurrency[]
                {
                    new PoeCurrency {Name = "Test currency 1", CodeName = "TC1"},
                    new PoeCurrency {Name = "Test currency 2", CodeName = "TC2"}
                },
            };
        }

        private IPoeQueryResult ProcessQuery(IPoeQueryInfo query)
        {
            return new PoeQueryResult
            {
                ItemsList = CreateFakeItems(15)
            };
        }

        private IPoeItem[] CreateFakeItems(int count)
        {
            return Enumerable
                .Range(0, count)
                .Select(CreateFakeItem)
                .ToArray();
        }

        private IPoeItem CreateFakeItem(int idx)
        {
            return new PoeItem
            {
                Price = $"10 TC{idx}",
                ItemName = $"Test item {idx}",
                League = $"Test league",
                UserForumUri = $"UserForumUri {idx}",
                UserIgn = $"UserIgn {idx}"
            };
        }
    }
}
