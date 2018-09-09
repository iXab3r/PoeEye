using System.Windows;
using PoeShared.Modularity;
using PoeShared.Native;

namespace PoeShared.PoeControlPanel.Modularity
{
    internal sealed class PoeControlPanelConfig : IPoeEyeConfigVersioned, IOverlayConfig
    {
        public bool IsEnabled { get; set; } = true;

        public Point OverlayLocation { get; set; }

        public Size OverlaySize { get; set; }

        public float OverlayOpacity { get; set; }

        public int Version { get; set; } = 3;
    }
}