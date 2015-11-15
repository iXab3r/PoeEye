namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System.Collections.Generic;

    using JetBrains.Annotations;

    using PoeShared.Common;

    internal interface IHistoricalTradesViewModel
    {
        bool IsExpanded { get; }

        IPoeItem[] Items { [NotNull] get; } 

        void AddItems(params IPoeItem[] items);

        void Clear();
    }
}