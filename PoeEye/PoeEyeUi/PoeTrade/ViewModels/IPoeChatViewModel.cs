namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.ObjectModel;

    using JetBrains.Annotations;

    using PoeShared.Chat;

    internal interface IPoeChatViewModel : IDisposable
    {
        ObservableCollection<PoeMessage> Messages { [NotNull] get; }
    }
}