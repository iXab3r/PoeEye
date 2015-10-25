namespace PoeEyeUi.Converters
{
    using Guards;

    using PoeShared.Common;
    using PoeShared.PoeTrade;

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

            return query;
        }
    }
}