using System.Windows.Input;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Collections.ObjectModel;

    using JetBrains.Annotations;

    using PoeWhisperMonitor.Chat;

    internal interface IPoeChatViewModel : IDisposable
    {
        ObservableCollection<PoeMessage> Messages { [NotNull] get; }

        ICommand SendMessageCommand { [NotNull] get; }
    }
}