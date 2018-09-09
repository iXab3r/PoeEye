using System.Collections.Generic;
using Guards;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PoeEye.StashRealtimeApi.API;
using PoeShared.Common;
using PoeShared.Prism;
using PoeShared.StashApi.DataTypes;
using TypeConverter;

namespace PoeEye.StashRealtimeApi.Models
{
    internal sealed class PoeItemFactory : IFactory<IPoeItem, IStashItem, StashTab>
    {
        private readonly IConverter<IStashItem, PoeItem> itemConverter;
        private readonly IConverter<string, PoePrice> priceConverter;

        public PoeItemFactory(
            [NotNull] IConverter<string, PoePrice> priceConverter,
            [NotNull] IConverter<IStashItem, PoeItem> itemConverter)
        {
            Guard.ArgumentNotNull(itemConverter, nameof(itemConverter));
            Guard.ArgumentNotNull(priceConverter, nameof(priceConverter));

            this.itemConverter = itemConverter;
            this.priceConverter = priceConverter;
        }

        public IPoeItem Create(IStashItem stashItem, StashTab stashTab)
        {
            Guard.ArgumentNotNull(stashItem, nameof(stashItem));
            Guard.ArgumentNotNull(stashTab, nameof(stashTab));

            var poeItem = itemConverter.Convert(stashItem);

            if (poeItem.Price == null && !string.IsNullOrWhiteSpace(stashTab.Stash))
            {
                var itemPrice = priceConverter.Convert(stashTab.Stash);
                if (itemPrice.IsEmpty)
                {
                    poeItem.Price = itemPrice.ToString();
                }
            }

            poeItem.UserForumName = stashTab.AccountName;
            poeItem.UserIgn = stashTab.LastCharacterName;
            poeItem.TabName = stashTab.Stash;
            stashTab.Items = new List<StashItem> {(StashItem)stashItem};
            poeItem.Raw = JsonConvert.SerializeObject(stashTab, Formatting.Indented);

            return poeItem;
        }
    }
}