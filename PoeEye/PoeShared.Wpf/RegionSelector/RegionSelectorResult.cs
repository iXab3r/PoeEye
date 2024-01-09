using System.Drawing;
using PoeShared.Native;

namespace PoeShared.RegionSelector;

public sealed record RegionSelectorResult
{
    public IWindowHandle Window { get; set; }
        
    public WinRect Selection { get; set; }
        
    public WinRect AbsoluteSelection { get; set; }
        
    public WpfRect WindowBounds { get; set; }
    
    public WpfRect TitleBarBounds { get; set; }
    
    public string Reason { get; set; }

    public bool IsValid => Selection is {Width: > 0, Height: > 0} && Window != null;
}