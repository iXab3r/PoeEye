using Guards;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    public sealed class StashUpdate 
    {
        public static StashUpdate Empty = new StashUpdate(new IStashItem[0], new IStashTab[0]);

        public StashUpdate(IStashItem[] items, IStashTab[] tabs)
        {
            Guard.ArgumentNotNull(() => items);
            Guard.ArgumentNotNull(() => tabs);

            Items = items;
            Tabs = tabs;
        }

        public IStashItem[] Items { get; } 

        public IStashTab[] Tabs { get; } 
    }
}