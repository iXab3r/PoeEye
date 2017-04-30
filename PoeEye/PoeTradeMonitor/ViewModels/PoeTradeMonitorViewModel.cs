using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class PoeTradeMonitorViewModel : OverlayViewModelBase
    {
        private readonly DelegateCommand<INegotiationViewModel> closeNegotiationCommand;
        private readonly IConfigProvider<PoeTradeMonitorConfig> configProvider;
        private readonly IOverlayWindowController controller;
        private readonly FakeItemFactory fakeItemFactory;
        private readonly IKeyboardEventsSource keyboardMouseEvents;

        private readonly SerialDisposable lifeCycleAnchors = new SerialDisposable();

        private readonly ISourceList<INegotiationViewModel> negotiationsList = new SourceList<INegotiationViewModel>();
        private readonly IFactory<INegotiationViewModel, TradeModel> notificationFactory;
        private readonly IFactory<ITradeMonitorService> tradeMonitorServiceFactory;
        private readonly IScheduler uiScheduler;

        private bool expandOnHover;

        private bool isExpanded;

        private int numberOfNegotiationsToExpandByDefault;

        private float opacity;

        private int preGroupNotificationsCount;

        public PoeTradeMonitorViewModel(
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IPoeStashService stashService,
            [NotNull] IOverlayWindowController controller,
            [NotNull] IFactory<INegotiationViewModel, TradeModel> notificationFactory,
            [NotNull] IFactory<ITradeMonitorService> tradeMonitorServiceFactory,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> configProvider,
            [NotNull] FakeItemFactory fakeItemFactory,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(stashService, nameof(stashService));
            Guard.ArgumentNotNull(controller, nameof(controller));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(notificationFactory, nameof(notificationFactory));
            Guard.ArgumentNotNull(tradeMonitorServiceFactory, nameof(tradeMonitorServiceFactory));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            MinSize = new Size(500, double.NaN);
            MaxSize = new Size(700, double.NaN);
            Height = double.NaN;
            SizeToContent = SizeToContent.Height;

            this.keyboardMouseEvents = keyboardMouseEvents;
            this.controller = controller;
            this.notificationFactory = notificationFactory;
            this.tradeMonitorServiceFactory = tradeMonitorServiceFactory;
            this.configProvider = configProvider;
            this.fakeItemFactory = fakeItemFactory;
            this.uiScheduler = uiScheduler;

            CreateFakeTradeCommand = new DelegateCommand(CreateFakeCommandExecuted);
            closeNegotiationCommand = new DelegateCommand<INegotiationViewModel>(CloseNegotiationCommandExecuted);
            LockWindowCommand = new DelegateCommand(LockWindowCommandExecuted);

            //FIXME Spaghetti code
            WhenLoaded.Subscribe(
                () =>
                {
                    var pageSizeObservable =
                        Observable.Merge(
                                this.WhenAnyValue(x => x.PreGroupNotificationsCount).ToUnit(),
                                this.WhenAnyValue(x => x.IsExpanded).ToUnit())
                            .Select(x => IsExpanded ? int.MaxValue : PreGroupNotificationsCount)
                            .Select(x => new VirtualRequest(0, x));

                    this.WhenAnyValue(x => x.GrowUpwards)
                        .Select(x => negotiationsList.Connect())
                        .Do(_ => Negotiations.Clear())
                        .Select(x => x.Virtualise(pageSizeObservable))
                        .Select(x => GrowUpwards ? x.Reverse() : x)
                        .Select(x => x.ObserveOn(uiScheduler).Bind(Negotiations))
                        .Switch()
                        .Subscribe()
                        .AddTo(Anchors);

                    var tradeMonitorService = tradeMonitorServiceFactory.Create();
                    tradeMonitorService.AddTo(Anchors);

                    tradeMonitorService
                        .Trades
                        .ObserveOn(uiScheduler)
                        .Subscribe(ProcessMessage)
                        .AddTo(Anchors);

                    keyboardMouseEvents
                        .WhenKeyDown
                        .Where(x => controller.IsVisible)
                        .Where(x => new KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt).MatchesHotkey(x))
                        .Do(x => x.Handled = true)
                        .Subscribe(ToggleLock, Log.HandleException)
                        .AddTo(Anchors);

                    configProvider
                        .WhenChanged
                        .Subscribe(ApplyConfig)
                        .AddTo(Anchors);

                    Observable.Merge(
                            this.WhenAnyValue(x => x.PreGroupNotificationsCount).ToUnit(),
                            negotiationsList.CountChanged.ToUnit()
                        )
                        .Subscribe(() => this.RaisePropertyChanged(nameof(NegotiationsOverflow)))
                        .AddTo(Anchors);
                }).AddTo(Anchors);
        }

        public IObservableCollection<INegotiationViewModel> Negotiations { get; } = new ObservableCollectionExtended<INegotiationViewModel>();

        public ICommand CloseNegotiationCommand => closeNegotiationCommand;

        public ICommand CreateFakeTradeCommand { get; }

        public ICommand LockWindowCommand { get; }

        public bool ExpandOnHover
        {
            get { return expandOnHover; }
            set { this.RaiseAndSetIfChanged(ref expandOnHover, value); }
        }

        public int NumberOfNegotiationsToExpandByDefault
        {
            get { return numberOfNegotiationsToExpandByDefault; }
            set { this.RaiseAndSetIfChanged(ref numberOfNegotiationsToExpandByDefault, value); }
        }
        
        public int PreGroupNotificationsCount
        {
            get { return preGroupNotificationsCount; }
            set { this.RaiseAndSetIfChanged(ref preGroupNotificationsCount, value); }
        }

        public bool IsExpanded
        {
            get { return isExpanded; }
            set { this.RaiseAndSetIfChanged(ref isExpanded, value); }
        }

        public int NegotiationsOverflow => negotiationsList.Count - PreGroupNotificationsCount;

        public float Opacity
        {
            get { return opacity; }
            set { this.RaiseAndSetIfChanged(ref opacity, value); }
        }

        private void ToggleLock()
        {
            if (!IsLocked)
            {
                Log.Instance.Debug($"[PoeTradeMonitorViewModel.ToggleLock] Locking window");
                LockWindowCommandExecuted();
            }
            else
            {
                Log.Instance.Debug($"[PoeTradeMonitorViewModel.ToggleLock] Unlocking window");
                UnlockWindowCommandExecuted();
            }
        }

        private void ProcessMessage(TradeModel model)
        {
            var existingModel = negotiationsList.Items
                .FirstOrDefault(x => TradeModel.Comparer.Equals(x.Negotiation, model));
            if (existingModel == null)
            {
                AddNegotiation(model);
            }
            else
            {
                UpdateNegotiation(existingModel, model);
            }
        }

        private void UpdateNegotiation(INegotiationViewModel viewModel, TradeModel model)
        {
            Log.Instance.Debug($"[PoeTradeMonitorViewModel.UpdateNegotiation] Updating existing negotiation: {viewModel}");
            viewModel.UpdateModel(model);
        }

        private void AddNegotiation(TradeModel model)
        {
            Log.Instance.Debug($"[PoeTradeMonitorViewModel.AddNegotiation] New trade: {model}");
            var newNegotiaton = notificationFactory.Create(model);
            var closeController = new NegotiationCloseController(this, newNegotiaton);
            Log.Instance.Debug($"[PoeTradeMonitorViewModel.AddNegotiation] Negotiation model: {newNegotiaton}");

            newNegotiaton.SetCloseController(closeController);

            negotiationsList.Add(newNegotiaton);

            if (!ExpandOnHover)
            {
                var expandedItemsCount = negotiationsList.Items.Count(x => x.IsExpanded);
                if (expandedItemsCount < NumberOfNegotiationsToExpandByDefault)
                {
                    newNegotiaton.IsExpanded = true;
                }
            }
        }

        private void CloseNegotiationCommandExecuted(INegotiationViewModel negotiation)
        {
            Log.Instance.Debug($"[PoeTradeMonitorViewModel.CloseNegotiation] Closing negotiation {negotiation}");
            if (negotiation == null)
            {
                return;
            }
            negotiationsList.Remove(negotiation);
            negotiation.Dispose();
        }

        private void CreateFakeCommandExecuted()
        {
            var fake = fakeItemFactory.Create();
            ProcessMessage(fake);
        }

        private void ApplyConfig(PoeTradeMonitorConfig config)
        {
            Log.Instance.Debug($"[PoeTradeMonitorViewModel.ApplyConfig] Config has changed");

            GrowUpwards = config.GrowUpwards;
            NumberOfNegotiationsToExpandByDefault = config.NumberOfNegotiationsToExpandByDefault;
            PreGroupNotificationsCount = config.PreGroupNotificationsCount;

            if (config.OverlaySize.Height <= 0 || config.OverlaySize.Width <= 0)
            {
                IsLocked = false;
                config.OverlaySize = MinSize;
            }
            Width = config.OverlaySize.Width;

            if (config.OverlayLocation.X <= 1 && config.OverlayLocation.Y <= 1)
            {
                IsLocked = false;
                config.OverlayLocation = new Point(Width / 2, Height / 2);
            }
            Left = config.OverlayLocation.X;

            if (GrowUpwards)
            {
                var deltaHeight = ActualHeight - config.OverlaySize.Height;
                Top = config.OverlayLocation.Y - deltaHeight;
            }
            else
            {
                Top = config.OverlayLocation.Y;
            }

            if (config.OverlayOpacity <= 0.01)
            {
                IsLocked = false;
                config.OverlayOpacity = 1;
            }
            Opacity = config.OverlayOpacity;
            ExpandOnHover = config.ExpandOnHover;
        }

        private void LockWindowCommandExecuted()
        {
            var config = configProvider.ActualConfig;
            config.OverlayLocation = new Point(Left, Top);
            config.GrowUpwards = GrowUpwards;
            config.NumberOfNegotiationsToExpandByDefault = NumberOfNegotiationsToExpandByDefault;
            config.OverlaySize = new Size(Width, Height);
            config.OverlayOpacity = Opacity;
            config.PreGroupNotificationsCount = PreGroupNotificationsCount;
            config.ExpandOnHover = ExpandOnHover;
            configProvider.Save(config);
            IsLocked = true;
        }

        private void UnlockWindowCommandExecuted()
        {
            IsLocked = false;
        }

        private class NegotiationCloseController : INegotiationCloseController
        {
            private readonly INegotiationViewModel negotiation;
            private readonly PoeTradeMonitorViewModel owner;

            public NegotiationCloseController(
                PoeTradeMonitorViewModel owner,
                INegotiationViewModel negotiation)
            {
                this.owner = owner;
                this.negotiation = negotiation;
            }

            public void Close()
            {
                owner.CloseNegotiationCommandExecuted(negotiation);
            }
        }
    }
}
