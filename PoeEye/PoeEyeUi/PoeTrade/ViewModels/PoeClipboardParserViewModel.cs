namespace PoeEyeUi.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Windows;

    using Factory;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.Utilities;

    using Prism;

    using ReactiveUI;

    using TypeConverter;

    using WpfClipboardMonitor;

    internal sealed class PoeClipboardParserViewModel : DisposableReactiveObject
    {
        private static readonly TimeSpan IsBusyThrottlingPeriod = TimeSpan.FromSeconds(1);

        private readonly IPoeItemParser itemParser;
        private readonly IConverter<IPoeItem, IPoeQueryInfo> itemToQueryConverter;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;

        private bool isBusyInternal;

        private bool isOpen;

        private IPoeTradeViewModel itemFromClipboard;
        private IPoeQueryInfo itemQueryInfo;

        public PoeClipboardParserViewModel(
            [NotNull] IPoeItemParser itemParser,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] IConverter<IPoeItem, IPoeQueryInfo> itemToQueryConverter,
            [NotNull] [Dependency(WellKnownSchedulers.Ui)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => itemParser);
            Guard.ArgumentNotNull(() => poeTradeViewModelFactory);
            Guard.ArgumentNotNull(() => itemToQueryConverter);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);

            this.itemParser = itemParser;
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;
            this.itemToQueryConverter = itemToQueryConverter;

            this.WhenAnyValue(x => x.ItemFromClipboard)
                .Subscribe(() => this.RaisePropertyChanged(nameof(ItemFromClipboard)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsBusyInternal)
                .Throttle(IsBusyThrottlingPeriod)
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(Anchors);

            var textFromClipboard = Observable
                .FromEventPattern<EventHandler, EventArgs>(
                    h => ClipboardNotifications.ClipboardUpdate += h,
                    h => ClipboardNotifications.ClipboardUpdate -= h)
                .Select(x => GetTextFromClipboard())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Publish();

            textFromClipboard
                .Do(_ => IsBusyInternal = true)
                .Do(_ => IsOpen = true)
                .Select(clipboardContent => Observable.Start(() => ParseItemData(clipboardContent), bgScheduler).Catch<IPoeItem, Exception>(HandleException))
                .Switch()
                .Select(item => new {Query = ConvertToQuery(item), Item = item})
                .ObserveOn(uiScheduler)
                .Do(_ => IsBusyInternal = false)
                .Subscribe(x => SetItemViewModel(x.Item, x.Query))
                .AddTo(Anchors);

            textFromClipboard.Connect();
        }

        public IPoeTradeViewModel ItemFromClipboard
        {
            get { return itemFromClipboard; }
            set { this.RaiseAndSetIfChanged(ref itemFromClipboard, value); }
        }

        public IPoeQueryInfo ItemQueryInfo
        {
            get { return itemQueryInfo; }
            set { this.RaiseAndSetIfChanged(ref itemQueryInfo, value); }
        }

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public bool IsBusy => IsBusyInternal;

        private bool IsBusyInternal
        {
            get { return isBusyInternal; }
            set { this.RaiseAndSetIfChanged(ref isBusyInternal, value); }
        }

        private IObservable<IPoeItem> HandleException(Exception exception)
        {
            Log.HandleException(exception);
            return Observable.Return(default(IPoeItem));
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
                Log.HandleUiException(ex);
                return null;
            }
        }

        private IPoeItem ParseItemData(string serializedData)
        {
            Guard.ArgumentNotNull(() => serializedData);
            var item = itemParser.Parse(serializedData);
            return item;
        }

        private IPoeQueryInfo ConvertToQuery(IPoeItem item)
        {
            return item == null
                ? null
                : itemToQueryConverter.Convert(item);
        }

        private void SetItemViewModel(IPoeItem item, IPoeQueryInfo query)
        {
            if (item == null)
            {
                ItemFromClipboard = null;
                ItemQueryInfo = null;
                IsOpen = false;
            }
            else
            {
                var trade = poeTradeViewModelFactory.Create(item);
                ItemFromClipboard = trade;
                ItemQueryInfo = query;
            }
        }
    }
}