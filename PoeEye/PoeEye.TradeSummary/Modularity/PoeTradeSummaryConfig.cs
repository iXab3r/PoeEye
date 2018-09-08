using System.Windows;
using PoeShared.Modularity;
using PoeShared.Native;

namespace PoeEye.TradeSummary.Modularity
{
    public class PoeTradeSummaryConfig : IPoeEyeConfigVersioned, IOverlayConfig
    {
        public bool IsEnabled { get; set; } = true;
        
        public Point OverlayLocation { get; set; }
        
        public Size OverlaySize { get; set; }

        public float OverlayOpacity { get; set; }

        public int Version { get; set; } = 3;
    }
}