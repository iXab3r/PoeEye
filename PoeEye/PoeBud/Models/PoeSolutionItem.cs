using PoeShared.Common;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    internal class PoeSolutionItem : IPoeSolutionItem
    {
        public PoeSolutionItem(IStashItem item, IStashTab tab)
        {
            Name = item.Name;
            TypeLine = item.TypeLine;
            Position = item.Position;
            Tab = tab;
            ItemType = item.ItemType;
            StackSize = item.StackSize;
        }

        public string Name { get; }

        public int StackSize { get; }

        public string TypeLine { get; }

        public ItemPosition Position { get; }

        public IStashTab Tab { get; }

        public GearType ItemType { get; }

        public override string ToString()
        {
            var name = !string.IsNullOrEmpty(Name)
                ? Name
                : TypeLine;

            return $"{name}({TypeLine}) @ {Tab.Name}-X{Position.X}Y{Position.Y}";
        }
    }
}