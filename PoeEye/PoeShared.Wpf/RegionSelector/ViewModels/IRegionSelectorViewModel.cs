using System;
using System.Drawing;
using JetBrains.Annotations;
using PoeShared.Scaffolding;

namespace PoeShared.RegionSelector.ViewModels
{
    public interface IRegionSelectorViewModel : IDisposableReactiveObject
    {
        RegionSelectorResult SelectionCandidate { [CanBeNull] get; }
        
        ISelectionAdornerViewModel SelectionAdorner { [NotNull] get; }

        [NotNull]
        IObservable<RegionSelectorResult> SelectWindow(Size minSelection);
    }
}