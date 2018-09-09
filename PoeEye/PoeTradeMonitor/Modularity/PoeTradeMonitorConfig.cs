using System.Collections.Generic;
using System.Windows;
using PoeEye.TradeMonitor.Models;
using PoeShared.Audio;
using PoeShared.Modularity;
using PoeShared.Native;

namespace PoeEye.TradeMonitor.Modularity
{
    public sealed class PoeTradeMonitorConfig : IPoeEyeConfigVersioned, IOverlayConfig, ICanBeEnabled
    {
        public static double MaxNumberOfNegotiationsToExpand = 10;

        public static PoeTradeMonitorConfig Default = new PoeTradeMonitorConfig
        {
            PredefinedMessages = new List<MacroMessage>
            {
                new MacroMessage {Label = "1m", Text = "one minute"},
                new MacroMessage {Label = "thx", Text = "thanks /kick /close"},
                new MacroMessage {Label = "no thx", Text = "no thanks /close"},
                new MacroMessage {Label = "sold", Text = "sold /close"},
                new MacroMessage {Label = "hideout", Text = "/hideout"}
            }
        };

        public int NumberOfNegotiationsToExpandByDefault { get; set; } = 1;

        public int PreGroupNotificationsCount { get; set; } = 3;

        public bool GrowUpwards { get; set; }

        public bool ExpandOnHover { get; set; }

        public List<MacroMessage> PredefinedMessages { get; set; } = new List<MacroMessage>();

        public AudioNotificationType NotificationType { get; set; } = AudioNotificationType.Mercury;

        public string CriticalNotificationEmailAddress { get; set; }

        public int CriticalNotificationThresholdInChaos { get; set; }
        public bool IsEnabled { get; set; } = true;

        public Point OverlayLocation { get; set; }

        public Size OverlaySize { get; set; }

        public float OverlayOpacity { get; set; } = 1;

        public int Version { get; set; } = 2;
    }
}