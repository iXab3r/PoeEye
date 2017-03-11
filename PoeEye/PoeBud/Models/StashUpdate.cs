using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    internal sealed class StashUpdate 
    {
        public StashUpdate(IStashItem[] items, IStashTab[] tabs)
        {
            Items = items;
            Tabs = tabs;
        }

        public IStashItem[] Items { get; } 

        public IStashTab[] Tabs { get; } 
    }
}