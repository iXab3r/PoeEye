using System;
using System.Drawing;
using System.Threading.Tasks;

namespace PoeShared.RegionSelector.Services;

[Obsolete("Must be migrated to WindowFinder")]
internal interface IScreenRegionSelectorService
{
    Task<RegionSelectorResult> SelectRegion(WinSize minSelection);
}