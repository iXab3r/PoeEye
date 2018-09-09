using JetBrains.Annotations;
using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    internal interface IPoeTradeSolution
    {
        IPoeSolutionItem[] Items { [NotNull] get; }

        IStashTab[] Tabs { [NotNull] get; }
    }
}