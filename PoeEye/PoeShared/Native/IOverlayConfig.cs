using System.Windows;
using PoeShared.Modularity;

namespace PoeShared.Native
{
    public interface IOverlayConfig : IPoeEyeConfig
    {
        Point OverlayLocation { get; set; }

        Size OverlaySize { get; set; }

        float OverlayOpacity { get; set; }
    }
}