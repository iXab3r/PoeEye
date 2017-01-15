using PoeBud.OfficialApi.DataTypes;

namespace PoeBud.Models
{
    internal sealed class StashUpdate 
    {
        public StashUpdate(IItem[] items, ITab[] tabs)
        {
            Items = items;
            Tabs = tabs;
        }

        public IItem[] Items { get; } 

        public ITab[] Tabs { get; } 
    }
}