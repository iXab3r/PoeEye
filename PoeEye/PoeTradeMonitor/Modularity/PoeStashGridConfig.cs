using System.Windows;
using PoeShared.Modularity;

namespace PoeEye.TradeMonitor.Modularity
{
    public class PoeStashGridConfig : IPoeEyeConfigVersioned
    {
        public Point OverlayLocation { get; set; }

        public Size OverlaySize { get; set; }

        public float OverlayOpacity { get; set; }

        public int Version { get; } = 1;
    }
}