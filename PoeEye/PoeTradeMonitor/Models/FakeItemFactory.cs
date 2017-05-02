using System;
using System.Linq;
using Guards;
using PoeShared;
using PoeShared.Common;
using PoeShared.Converters;
using PoeShared.PoeDatabase;
using PoeShared.Scaffolding;

namespace PoeEye.TradeMonitor.Models
{
    internal sealed class FakeItemFactory
    {
        private const int MaxInventoryWidth = 12;
        private const int MaxInventoryHeight = 12;

        private readonly IPoeDatabaseReader database;
        private readonly IClock clock;

        private readonly Random rng = new Random();

        public FakeItemFactory(
            IPoeDatabaseReader database,
            IClock clock)
        {
            Guard.ArgumentNotNull(database, nameof(database));
            Guard.ArgumentNotNull(clock, nameof(clock));

            this.database = database;
            this.clock = clock;
        }

        public TradeModel Create()
        {

            return new TradeModel
            {
                Timestamp = clock.Now,
                CharacterName = "Xaber",
                ItemPosition = new ItemPosition(rng.Next(0, MaxInventoryWidth), rng.Next(0, MaxInventoryHeight), rng.Next(1,2), rng.Next(1,3)),
                TradeType = rng.Next(0,100) >= 70 ? TradeType.Buy : TradeType.Sell,
                League = "League",
                Price = GetRandomPrice(),
                TabName = $"Tab #{rng.Next(1,100)}",
                Offer = rng.Next(0, 100) >= 70 ? $"offer {GetRandomPrice()}" : null,
                PositionName = database.KnownEntityNames.PickRandom()
            };
        }

        private PoePrice GetRandomPrice()
        {
            return new PoePrice(GetRandomCurrency(), rng.Next(0, 100));
        }

        private string GetRandomCurrency()
        {
            var knownCurrencies = KnownCurrencyNameList.CurrencyByAlias;

            return knownCurrencies.PickRandom().Value;
        }
    }
}
