using System.Windows;
using PoeShared.Modularity;

namespace PoeEye.TradeMonitor.Modularity
{
    public class PoeControlPanelConfig : IPoeEyeConfigVersioned
    {
        public Point OverlayLocation { get; set; }

        public float OverlayOpacity { get; set; }

        public int Version { get; set; } = 2;
    }
}