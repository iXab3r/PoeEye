using PoeShared.Common;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    using JetBrains.Annotations;

    internal interface IPoeSolutionItem
    {
        string Name { [NotNull] get; }
        
        int StackSize { [NotNull] get; }
        
        string TypeLine { [NotNull] get; }

        ItemPosition Position { get; }

        IStashTab Tab { [NotNull] get; }

        GearType ItemType { get; }
    }
}