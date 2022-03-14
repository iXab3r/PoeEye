using System.Drawing;
using PoeShared.Modularity;

namespace PoeShared.Native;

public interface IOverlayConfig : IPoeEyeConfig
{
    Rectangle OverlayBounds { get; set; }

    float OverlayOpacity { get; set; }
}