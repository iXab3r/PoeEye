

namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;

    using JetBrains.Annotations;
    using System.Collections.ObjectModel;
    using PoeShared.Chat;

    internal interface IPoeChatViewModel : IDisposable
    {
        ObservableCollection<PoeMessage> Messages { [NotNull] get; }
    }
}