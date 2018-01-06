using JetBrains.Annotations;
using PoeBud.Models;
using PoeShared.Common;

namespace PoeBud.ViewModels {
    internal interface IPriceSummaryViewModel {
        IPoeTradeSolution Solution { [CanBeNull] get; [CanBeNull] set; }
        PoePrice PriceInChaosOrbs { get; set; }
    }
}