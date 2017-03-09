using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using PoeShared.Modularity;

namespace PoeEye.TradeMonitor.Modularity
{
    public class PoeTradeMonitorConfig : IPoeEyeConfig
    {
        public static double MaxNumberOfNegotiationsToExpand = 10;

        public int NumberOfNegotiationsToExpandByDefault { get; set; } = 1;

        public bool GrowUpwards { get; set; }

        public float OverlayOpacity { get; set; }

        public Point OverlayLocation { get; set; }

        public Size OverlaySize { get; set; }

        public List<MacroMessage> PredefinedMessages { get; set; }

        public static PoeTradeMonitorConfig Default = new PoeTradeMonitorConfig()
        {
            PredefinedMessages = new List<MacroMessage>()
            {
                new MacroMessage { Label = "1m", Text = "one minute" },
                new MacroMessage { Label = "thx", Text = "thanks" },
                new MacroMessage { Label = "no thx", Text = "no thanks" },
                new MacroMessage { Label = "sold", Text = "sold" },
            }
        };
    }
}