namespace PoeEye.Simulator
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;

    using Common;

    using Guards;

    using PoeShared;
    using PoeShared.Query;

    internal sealed class PoeApi : IPoeApi
    {
        public IObservable<IPoeQueryResult> IssueQuery(IPoeQuery query)
        {
            Guard.ArgumentNotNull(() => query);
            
            return Observable.Return(ProcessQuery(query));
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