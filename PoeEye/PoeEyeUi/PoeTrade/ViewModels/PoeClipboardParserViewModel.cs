namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Windows;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.Utilities;

    using Prism;

    using ReactiveUI;

    using WpfClipboardMonitor;

    internal sealed class PoeClipboardParserViewModel : DisposableReactiveObject
    {
        private readonly IPoeItemParser itemParser;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;

        private bool isBusy;

        private bool isOpen;
        private IPoeTradeViewModel itemFromClipboard;

        public PoeClipboardParserViewModel(
            [NotNull] IPoeItemParser itemParser,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => itemParser);
            Guard.ArgumentNotNull(() => poeTradeViewModelFactory);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);

            this.itemParser = itemParser;
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;

            Observable
                .FromEventPattern<EventHandler, EventArgs>(
                    h => ClipboardNotifications.ClipboardUpdate += h,
                    h => ClipboardNotifications.ClipboardUpdate -= h)
                .Select(x => GetTextFromClipboard())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Do(_ => IsBusy = true)
                .Do(_ => IsOpen = true)
                .Select(
                    data => Observable.Start(() => GetItemFromText(data), bgScheduler)
                        .Catch<IPoeItem, Exception>(HandleParsingException))
                .Switch()
                .ObserveOn(uiScheduler)
                .Do(SetItem)
                .Do(_ => IsBusy = false)
                .Subscribe()
                .AddTo(Anchors);
        }

        private IObservable<IPoeItem> HandleParsingException(Exception exception)
        {
            Log.HandleException(exception);
            return Observable.Return(default(IPoeItem));
        }

        public IPoeTradeViewModel ItemFromClipboard
        {
            get { return itemFromClipboard; }
            set { this.RaiseAndSetIfChanged(ref itemFromClipboard, value); }
        }

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        private string GetTextFromClipboard()
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    return null;
                }

                var textFromClipboard = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(textFromClipboard))
                {
                    return null;
                }

                return textFromClipboard;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                return null;
            }
        }

        private IPoeItem GetItemFromText(string serializedData)
        {
            Guard.ArgumentNotNull(() => serializedData);
            var item = itemParser.Parse(serializedData);
            return item;
        }

        private void SetItem(IPoeItem item)
        {
            if (item == null)
            {
                ItemFromClipboard = null;
                IsOpen = false;
            }
            else
            {
                var trade = poeTradeViewModelFactory.Create(item);
                ItemFromClipboard = trade;
                IsOpen = true;
            }
        }
    }
}