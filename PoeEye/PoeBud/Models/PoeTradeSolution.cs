using PoeShared.StashApi.DataTypes;

namespace PoeBud.Models {
    internal class PoeTradeSolution : IPoeTradeSolution
    {
        public PoeTradeSolution(IPoeSolutionItem[] items, IStashTab[] tabs)
        {
            Items = items;
            Tabs = tabs;
        }

        public IPoeSolutionItem[] Items { get; }

        public IStashTab[] Tabs { get; }
    }
}