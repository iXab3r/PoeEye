namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeShared.Scaffolding;

    using PoeWhisperMonitor;
    using PoeWhisperMonitor.Chat;

    using Prism;

    internal sealed class PoeChatViewModel : DisposableReactiveObject, IPoeChatViewModel
    {
        public PoeChatViewModel(
            [NotNull] IPoeWhisperService whisperService,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => whisperService);
            Guard.ArgumentNotNull(() => uiScheduler);

            whisperService.Messages
                    .ObserveOn(uiScheduler)
                    .Where(x => x.MessageType == PoeMessageType.Whisper)
                    .Subscribe(Messages.Add)
                    .AddTo(Anchors);
        }

        public ObservableCollection<PoeMessage> Messages { get; } = new ObservableCollection<PoeMessage>();
    }
}