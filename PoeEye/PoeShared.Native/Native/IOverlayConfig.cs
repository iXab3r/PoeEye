using System;
using System.Drawing;
using PoeShared.Modularity;

namespace PoeShared.Native;

public interface IOverlayConfig : IPoeEyeConfig
{
    [Obsolete]
    System.Windows.Point? OverlayLocation { get; set; }

    [Obsolete]
    System.Windows.Size? OverlaySize { get; set; }
        
    Rectangle OverlayBounds { get; set; }

    float OverlayOpacity { get; set; }
}