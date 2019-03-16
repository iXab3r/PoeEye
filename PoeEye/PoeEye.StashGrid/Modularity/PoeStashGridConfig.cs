using System.Windows;
using PoeShared.Modularity;
using PoeShared.Native;

namespace PoeEye.StashGrid.Modularity
{
    public class PoeStashGridConfig : IPoeEyeConfigVersioned, IOverlayConfig
    {
        public Rect StashBounds { get; set; }
        
        public Point OverlayLocation { get; set; }

        public Size OverlaySize { get; set; }

        public float OverlayOpacity { get; set; }

        public int Version { get; set; } = 2;
    }
}