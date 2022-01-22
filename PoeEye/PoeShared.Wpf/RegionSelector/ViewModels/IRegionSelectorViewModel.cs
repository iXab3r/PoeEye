using System.Drawing;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Native;
using WinPoint = System.Drawing.Point;

namespace PoeShared.RegionSelector.ViewModels;

public interface IRegionSelectorViewModel : IOverlayViewModel
{
    RegionSelectorResult SelectionCandidate { [CanBeNull] get; }
        
    Task<RegionSelectorResult> StartSelection(Size minSelection);
}