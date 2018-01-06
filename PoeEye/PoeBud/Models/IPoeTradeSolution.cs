using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models
{
    using JetBrains.Annotations;

    internal interface IPoeTradeSolution
    {
        IPoeSolutionItem[] Items { [NotNull] get; }

        IStashTab[] Tabs { [NotNull] get; }
    }
}