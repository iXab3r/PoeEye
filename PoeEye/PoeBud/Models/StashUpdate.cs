using System.Linq;
using Guards;
using JetBrains.Annotations;
using PoeBud.Scaffolding;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    public sealed class StashUpdate 
    {
        public static StashUpdate Empty = new StashUpdate(new IStashItem[0], new IStashTab[0]);

        public StashUpdate(IStashItem[] items, IStashTab[] tabs)
        {
            Guard.ArgumentNotNull(items, nameof(items));
            Guard.ArgumentNotNull(tabs, nameof(tabs));

            Items = items;
            Tabs = tabs;
        }

        public IStashItem[] Items { [NotNull] get; } 

        public IStashTab[] Tabs { [NotNull] get; }

        internal StashUpdate RemoveItems(IPoeSolutionItem[] solutionItems)
        {
            var dirtyItems = Items.Where(item => !solutionItems.Any(tradeItem => IsMatch(tradeItem, item))).ToArray();
            var result = new StashUpdate(dirtyItems, Tabs);
            return result;
        }
        
        private bool IsMatch(IPoeSolutionItem solutionItem, IStashItem item)
        {
            return solutionItem.Tab.GetInventoryId() == item.InventoryId && solutionItem.Position.X == item.Position.X && solutionItem.Position.Y == item.Position.Y;
        }
    }
}
