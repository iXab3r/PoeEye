using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Common.Logging;
using DynamicData;
using DynamicData.Binding;
using Guards;
using JetBrains.Annotations;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeEye.TradeMonitor.Services;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;
using Unity.Attributes;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class PoeTradeMonitorViewModel : OverlayViewModelBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PoeTradeMonitorViewModel));

        private readonly DelegateCommand<INegotiationViewModel> closeNegotiationCommand;
        private readonly IConfigProvider<PoeTradeMonitorConfig> configProvider;
        private readonly FakeItemFactory fakeItemFactory;

        private readonly ISourceList<INegotiationViewModel> negotiationsList = new SourceList<INegotiationViewModel>();
        private readonly IFactory<INegotiationViewModel, TradeModel> notificationFactory;
        private readonly IFactory<ITradeMonitorService> tradeMonitorServiceFactory;
        private readonly IScheduler uiScheduler;

        private bool expandOnHover;

        private bool isExpanded;

        private int numberOfNegotiationsToExpandByDefault;

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
            IsUnlockable = true;
            Title = "Trade Monitor";

            this.notificationFactory = notificationFactory;
            this.tradeMonitorServiceFactory = tradeMonitorServiceFactory;
            this.configProvider = configProvider;
            this.fakeItemFactory = fakeItemFactory;
            this.uiScheduler = uiScheduler;

            CreateFakeTradeCommand = new DelegateCommand(CreateFakeCommandExecuted);
            closeNegotiationCommand = new DelegateCommand<INegotiationViewModel>(CloseNegotiationCommandExecuted);
            CloseAllNegotiations = new DelegateCommand(CloseAllNegotiationsCommandExecuted);

            WhenLoaded.Subscribe(Initialize).AddTo(Anchors);
        }

        public IObservableCollection<INegotiationViewModel> Negotiations { get; } = new ObservableCollectionExtended<INegotiationViewModel>();

        public ICommand CloseNegotiationCommand => closeNegotiationCommand;

        public ICommand CreateFakeTradeCommand { get; }

        public ICommand CloseAllNegotiations { get; }

        public bool ExpandOnHover
        {
            get => expandOnHover;
            set => this.RaiseAndSetIfChanged(ref expandOnHover, value);
        }

        public int NumberOfNegotiationsToExpandByDefault
        {
            get => numberOfNegotiationsToExpandByDefault;
            set => this.RaiseAndSetIfChanged(ref numberOfNegotiationsToExpandByDefault, value);
        }

        public int PreGroupNotificationsCount
        {
            get => preGroupNotificationsCount;
            set => this.RaiseAndSetIfChanged(ref preGroupNotificationsCount, value);
        }

        public bool IsExpanded
        {
            get => isExpanded;
            set => this.RaiseAndSetIfChanged(ref isExpanded, value);
        }

        public int NegotiationsOverflow => negotiationsList.Count - PreGroupNotificationsCount;

        private void Initialize()
        {
            Log.Info("Initializing TradeMonitor...");

            var pageSizeObservable =
                this.WhenAnyValue(x => x.PreGroupNotificationsCount).ToUnit().Merge(this.WhenAnyValue(x => x.IsExpanded).ToUnit())
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

            configProvider
                .WhenChanged
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.PreGroupNotificationsCount).ToUnit().Merge(negotiationsList.CountChanged.ToUnit()
                )
                .Subscribe(() => this.RaisePropertyChanged(nameof(NegotiationsOverflow)))
                .AddTo(Anchors);
        }

        private void CloseAllNegotiationsCommandExecuted()
        {
            Log.Debug("Closing all negotiations");

            foreach (var negotiation in negotiationsList.Items)
            {
                CloseNegotiationCommandExecuted(negotiation);
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
            Log.Debug(
                $"Updating existing negotiation: {viewModel}");
            viewModel.UpdateModel(model);
        }

        private void AddNegotiation(TradeModel model)
        {
            Log.Debug($"New trade: {model}");
            var newNegotiaton = notificationFactory.Create(model);
            var closeController = new NegotiationCloseController(this, newNegotiaton);
            Log.Debug($"Negotiation model: {newNegotiaton}");

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
            Log.Debug($"Closing negotiation {negotiation}");
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
            Log.Debug("Config has changed");

            GrowUpwards = config.GrowUpwards;
            NumberOfNegotiationsToExpandByDefault = config.NumberOfNegotiationsToExpandByDefault;
            PreGroupNotificationsCount = config.PreGroupNotificationsCount;

            base.ApplyConfig(config);

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

            ExpandOnHover = config.ExpandOnHover;
        }

        protected override void LockWindowCommandExecuted()
        {
            base.LockWindowCommandExecuted();

            var config = configProvider.ActualConfig;
            SavePropertiesToConfig(config);

            config.GrowUpwards = GrowUpwards;
            config.NumberOfNegotiationsToExpandByDefault = NumberOfNegotiationsToExpandByDefault;
            config.PreGroupNotificationsCount = PreGroupNotificationsCount;
            config.ExpandOnHover = ExpandOnHover;
            configProvider.Save(config);
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