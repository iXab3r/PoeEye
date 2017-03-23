using JetBrains.Annotations;

namespace PoeEye.TradeMonitor.Services.Notifications
{
    internal interface IPoeNotifier
    {
        void SendNotification([NotNull] string message, NotificationLevel level);
    }
}