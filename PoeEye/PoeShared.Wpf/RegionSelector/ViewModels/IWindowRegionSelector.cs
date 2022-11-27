using System.Drawing;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Native;

namespace PoeShared.RegionSelector.ViewModels;

public interface IWindowRegionSelector : IOverlayViewModel
{
    RegionSelectorResult SelectionCandidate { [CanBeNull] get; }
        
    Task<RegionSelectorResult> StartSelection(Size minSelection);
}