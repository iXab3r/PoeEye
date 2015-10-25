namespace PoeEyeUi.Converters
{
    using System.Linq;

    using Guards;

    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.PoeTrade.Query;

    using TypeConverter;

    internal sealed class PoeItemToPoeQueryConverter : IConverter<IPoeItem, IPoeQueryInfo>
    {
        public IPoeQueryInfo Convert(IPoeItem value)
        {
            Guard.ArgumentNotNull(() => value);

            var query = new PoeQueryInfo()
            {
                ItemName = value.ItemName,

                BuyoutOnly = true,
                OnlineOnly = true,
                NormalizeQuality = true,
                IsExpanded = true
            };

            var implicitMod = value.Mods.SingleOrDefault(x => x.ModType == PoeModType.Implicit);
            if (implicitMod != null)
            {
                query.ImplicitMod = new PoeQueryRangeModArgument(implicitMod.CodeName);
            }

            var explicitMods = value.Mods.Where(x => x.ModType == PoeModType.Explicit).ToArray();
            query.ExplicitMods = explicitMods.Select(x => new PoeQueryRangeModArgument(x.CodeName)).ToArray();

            return query;
        }
    }
}