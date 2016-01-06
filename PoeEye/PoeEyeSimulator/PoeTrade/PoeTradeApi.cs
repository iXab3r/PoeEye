namespace PoeEye.Simulator.PoeTrade
{
    using System.Linq;
    using System.Threading.Tasks;

    using Guards;

    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    internal sealed class PoeTradeApi : IPoeApi
    {
        public Task<IPoeQueryResult> IssueQuery(IPoeQuery query)
        {
            Guard.ArgumentNotNull(() => query);
            
            return Task.Run(() => ProcessQuery(query));
        }

        public Task<IPoeQueryResult> GetStaticData()
        {
            return Task.Run(() => PrepareStatisData());
        }

        private IPoeQueryResult PrepareStatisData()
        {
            return new PoeQueryResult()
            {
                LeaguesList = new string[] { "League 1", "League 2" },
            };
        }

        private IPoeQueryResult ProcessQuery(IPoeQuery query)
        {
            return new PoeQueryResult()
            {
                ModsList = new IPoeItemMod[0],
                CurrenciesList = new IPoeCurrency[]
                {
                    new PoeCurrency() { Name = "Test currency 1", CodeName = "TC1" },
                    new PoeCurrency() { Name = "Test currency 2", CodeName = "TC2" },
                },
                ItemsList = CreateFakeItems(15),
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
            return new PoeItem()
            {
                Price = $"10 TC{idx}",
                ItemName = $"Test item {idx}",
                League = $"Test league",
                UserForumUri = $"UserForumUri {idx}",
                UserIgn = $"UserIgn {idx}",
            };
        }
    }
}