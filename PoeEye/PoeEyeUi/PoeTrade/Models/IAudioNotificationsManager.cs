using System.Windows.Input;

namespace PoeEyeUi.PoeTrade.Models
{
    using System;

    using JetBrains.Annotations;

    internal interface IAudioNotificationsManager : IDisposable
    {
        ICommand PlayNotificationCommand { [NotNull] get; }

        bool IsEnabled { get; set; }
    }
}