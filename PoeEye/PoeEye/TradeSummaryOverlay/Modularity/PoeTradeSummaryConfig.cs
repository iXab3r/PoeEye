using System.Windows;
using PoeShared.Modularity;
using PoeShared.Native;

namespace PoeEye.TradeSummaryOverlay.Modularity
{
    public class PoeTradeSummaryConfig : IPoeEyeConfigVersioned, IOverlayConfig
    {
        public bool IsEnabled { get; set; } = true;

        public bool IsVisible { get; set; } = true;

        public HorizontalAlignment ControlBarAlignment { get; set; } = HorizontalAlignment.Right;
        
        public Point OverlayLocation { get; set; }
        
        public Size OverlaySize { get; set; }

        public float OverlayOpacity { get; set; } = 1;

        public double ScaleFactor { get; set; } = 1;

        public int Version { get; set; } = 4;
    }
}