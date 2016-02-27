namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeShared.Chat;
    using PoeShared.Scaffolding;

    using PoeWhisperMonitor;

    using Prism;

    internal sealed class PoeChatViewModel : DisposableReactiveObject, IPoeChatViewModel
    {
        public PoeChatViewModel(
            [NotNull] IPoeWhispers whispers,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => whispers);
            Guard.ArgumentNotNull(() => uiScheduler);

            whispers.Messages
                    .ObserveOn(uiScheduler)
                    .Where(x => x.MessageType == PoeMessageType.Whisper)
                    .Subscribe(Messages.Add)
                    .AddTo(Anchors);
        }

        public ObservableCollection<PoeMessage> Messages { get; } = new ObservableCollection<PoeMessage>();
    }
}