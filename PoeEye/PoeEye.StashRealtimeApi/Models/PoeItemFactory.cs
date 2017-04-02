using Guards;
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

        public PoeItemFactory(IConverter<IStashItem, PoeItem> itemConverter)
        {
            Guard.ArgumentNotNull(() => itemConverter);

            this.itemConverter = itemConverter;
        }

        public IPoeItem Create(IStashItem stashItem, StashTab stashTab)
        {
            Guard.ArgumentNotNull(stashItem, nameof(stashItem));
            Guard.ArgumentNotNull(stashTab, nameof(stashTab));

            var poeItem = itemConverter.Convert(stashItem);

            poeItem.UserForumName = stashTab.AccountName;
            poeItem.TabName = stashTab.Stash;

            return poeItem;
        }
    }
}