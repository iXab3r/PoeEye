﻿using PoeEye.ItemParser;
using PoeEye.ItemParser.Services;
using PoeEye.PoeTrade.Common;
using PoeShared.Modularity;
using Prism.Unity;
using ReactiveUI.Legacy;
using PoeEyeMainConfig = PoeEye.Config.PoeEyeMainConfig;

namespace PoeEye.PoeTrade.ViewModels
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;
    using System.Windows;
    using System.Windows.Input;

    using ClipboardMonitor;

    using Config;

    using Exceptionless;

    using Guards;

    using JetBrains.Annotations;

    using Microsoft.Practices.Unity;

    using Models;

    using PoeEye.Prism;

    using PoeShared;
    using PoeShared.Common;
    using PoeShared.PoeTrade;
    using PoeShared.Prism;
    using PoeShared.Scaffolding;

    using ReactiveUI;

    using TypeConverter;
    using IPoeEyeMainConfigProvider = IConfigProvider<PoeEyeMainConfig>;

    internal sealed class PoeClipboardParserViewModel : DisposableReactiveObject
    {
        private static readonly TimeSpan IsBusyThrottlingPeriod = TimeSpan.FromSeconds(1);

        private readonly IPoeItemParser itemParser;
        private readonly IConverter<IPoeItem, IPoeQueryInfo> itemToQueryConverter;
        private readonly ReactiveCommand<object> parseClipboard;
        private readonly IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory;

        private bool isBusyInternal;

        private bool isOpen;

        private IPoeTradeViewModel itemFromClipboard;
        private IPoeQueryInfo itemQueryInfo;

        public PoeClipboardParserViewModel(
            [NotNull] IUnityContainer container,
            [NotNull] IFactory<IPoeTradeViewModel, IPoeItem> poeTradeViewModelFactory,
            [NotNull] IPoeEyeMainConfigProvider configProvider,
            [NotNull] [Dependency(WellKnownWindows.PathOfExileWindow)] IWindowTracker poeWindowTracker,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(container, nameof(container));
            Guard.ArgumentNotNull(poeTradeViewModelFactory, nameof(poeTradeViewModelFactory));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(poeWindowTracker, nameof(poeWindowTracker));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));

            itemParser = container.TryResolve<IPoeItemParser>();
            Log.Instance.Warn("[PoeClipboardParserViewModel..ctor] Item parser was not loaded");
            return;

            this.itemToQueryConverter = container.Resolve<IConverter<IPoeItem, IPoeQueryInfo>>();
            this.poeTradeViewModelFactory = poeTradeViewModelFactory;

            this.WhenAnyValue(x => x.IsBusyInternal).Throttle(IsBusyThrottlingPeriod).Merge(this.WhenAnyValue(x => x.IsOpen))
                .Subscribe(() => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(Anchors);

            parseClipboard = ReactiveUI.Legacy.ReactiveCommand.Create();

            var textFromClipboard = Observable
                .FromEventPattern<EventHandler, EventArgs>(
                    h => ClipboardNotifications.ClipboardUpdate += h,
                    h => ClipboardNotifications.ClipboardUpdate -= h)
                .Where(x => configProvider.ActualConfig.ClipboardMonitoringEnabled)
                .Merge(parseClipboard)
                .Where(x => poeWindowTracker.IsActive)
                .Select(x => GetTextFromClipboard())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Publish();

            textFromClipboard
                .Do(_ => SetItemViewModel(null, null))
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

        public ICommand ParseClipboard => parseClipboard;

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
            Guard.ArgumentNotNull(serializedData, nameof(serializedData));
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
                trade.TradeState = PoeTradeState.Normal;
                ItemFromClipboard = trade;
                ItemQueryInfo = query;

                ExceptionlessClient.Default
                    .CreateFeatureUsage("ClipboardParser")
                    .SetProperty("Item", item.DumpToText())
                    .Submit();
            }
        }
    }
}
