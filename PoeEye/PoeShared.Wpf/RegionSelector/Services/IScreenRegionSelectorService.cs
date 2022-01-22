using System.Drawing;
using System.Threading.Tasks;

namespace PoeShared.RegionSelector.Services;

public interface IScreenRegionSelectorService
{
    Task<RegionSelectorResult> SelectRegion(Size minSelection);
}