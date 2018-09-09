using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeWhisperMonitor.Chat;

namespace PoeEye.PoeTrade.ViewModels
{
    internal interface IPoeChatViewModel : IDisposable
    {
        ObservableCollection<PoeMessage> Messages { [NotNull] get; }

        ICommand SendMessageCommand { [NotNull] get; }
    }
}