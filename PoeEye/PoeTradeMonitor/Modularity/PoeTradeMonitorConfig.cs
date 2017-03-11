using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using PoeEye.TradeMonitor.Models;
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

        public List<MacroMessage> PredefinedMessages { get; set; } = new List<MacroMessage>();

        public static PoeTradeMonitorConfig Default = new PoeTradeMonitorConfig()
        {
            PredefinedMessages = new List<MacroMessage>()
            {
                new MacroMessage { Label = "1m", Text = "one minute" },
                new MacroMessage { Label = "thx", Text = "thanks /Kick /Close" },
                new MacroMessage { Label = "no thx", Text = "no thanks /Close" },
                new MacroMessage { Label = "sold", Text = "sold /Close" },
            }
        };
    }
}