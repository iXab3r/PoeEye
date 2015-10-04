using System.Windows.Input;

namespace PoeEyeUi.PoeTrade.Models
{
    using JetBrains.Annotations;

    internal interface IAudioNotificationsManager
    {
        ICommand PlayNotificationCommand { [NotNull] get; }

        bool IsEnabled { get; set; }
    }
}