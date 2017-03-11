using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeEye.TradeMonitor.Models;
using PoeEye.TradeMonitor.Modularity;
using PoeShared;
using PoeShared.Common;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using MouseEventHandler = System.Windows.Forms.MouseEventHandler;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class PoeTradeMonitorViewModel : OverlayViewModelBase
    {
        private readonly DelegateCommand<INegotiationViewModel> closeNegotiationCommand;
        private readonly IConfigProvider<PoeTradeMonitorConfig> configProvider;
        private readonly IOverlayWindowController controller;

        private readonly IFactory<INegotiationViewModel, TradeModel> notificationFactory;

        private bool growUpwards;

        public PoeTradeMonitorViewModel(
            [NotNull] IKeyboardMouseEvents keyboardMouseEvents,
            [NotNull] IPoeStashService stashService,
            [NotNull] IOverlayWindowController controller,
            [NotNull] IFactory<INegotiationViewModel, TradeModel> notificationFactory,
            [NotNull] ITradeMonitorService tradeMonitorService,
            [NotNull] IConfigProvider<PoeTradeMonitorConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => keyboardMouseEvents);
            Guard.ArgumentNotNull(() => stashService);
            Guard.ArgumentNotNull(() => controller);
            Guard.ArgumentNotNull(() => configProvider);
            Guard.ArgumentNotNull(() => notificationFactory);
            Guard.ArgumentNotNull(() => tradeMonitorService);
            Guard.ArgumentNotNull(() => bgScheduler);
            Guard.ArgumentNotNull(() => uiScheduler);

            MinSize = new Size(500, 300);
            MaxSize = new Size(700, double.NaN);

            this.controller = controller;
            this.notificationFactory = notificationFactory;
            this.configProvider = configProvider;

            tradeMonitorService
                .Trades
                .ObserveOn(uiScheduler)
                .Subscribe(ProcessTrade)
                .AddTo(Anchors);
            CreateFakeTradeCommand = new DelegateCommand(CreateFakeCommandExecuted);
            closeNegotiationCommand = new DelegateCommand<INegotiationViewModel>(CloseNegotiationCommandExecuted);
            LockWindowCommand = new DelegateCommand(LockWindowCommandExecuted);
            configProvider
                .WhenAnyValue(x => x.ActualConfig)
                .Subscribe(ApplyConfig)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.NumberOfNegotiationsToExpandByDefault)
                .Subscribe(x => configProvider.ActualConfig.NumberOfNegotiationsToExpandByDefault = NumberOfNegotiationsToExpandByDefault)
                .AddTo(Anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardMouseEvents.KeyDown += h,
                    h => keyboardMouseEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Where(x => controller.IsVisible)
                .Where(
                    x =>
                        new KeyGesture(Key.F8, ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)
                            .MatchesHotkey(x))
                .Do(x => x.Handled = true)
                .Subscribe(
                    () =>
                    {
                        if (!IsLocked)
                        {
                            LockWindowCommandExecuted();
                        }
                        else
                        {
                            UnlockWindowCommandExecuted();
                        }
                    })
                .AddTo(Anchors);
        }

        public IReactiveList<INegotiationViewModel> Negotiations { get; } = new ReactiveList<INegotiationViewModel>();

        public ICommand CloseNegotiationCommand => closeNegotiationCommand;

        public ICommand CreateFakeTradeCommand { get; }

        public ICommand LockWindowCommand { get; }

        public bool GrowUpwards
        {
            get { return growUpwards; }
            set { this.RaiseAndSetIfChanged(ref growUpwards, value); }
        }

        private int numberOfNegotiationsToExpandByDefault;

        public int NumberOfNegotiationsToExpandByDefault
        {
            get { return numberOfNegotiationsToExpandByDefault; }
            set { this.RaiseAndSetIfChanged(ref numberOfNegotiationsToExpandByDefault, value); }
        }

        private float opacity;

        public float Opacity
        {
            get { return opacity; }
            set { this.RaiseAndSetIfChanged(ref opacity, value); }
        }

        private void ProcessTrade(TradeModel model)
        {
            var newNegotiaton = notificationFactory.Create(model);
            var closeController = new NegotiationClassController(Negotiations, newNegotiaton);

            newNegotiaton.SetCloseController(closeController);

            Negotiations.Add(newNegotiaton);

            var expandedItemsCount = Negotiations.Count(x => x.IsExpanded);
            if (expandedItemsCount < configProvider.ActualConfig.NumberOfNegotiationsToExpandByDefault &&
                Negotiations.Count > 0)
            {
                newNegotiaton.IsExpanded = true;
            }
        }

        private void CloseNegotiationCommandExecuted(INegotiationViewModel negotiation)
        {
            if (negotiation == null)
            {
                return;
            }
            Negotiations.Remove(negotiation);
            negotiation.Dispose();
        }

        private void CreateFakeCommandExecuted()
        {
            var fake = new TradeModel
            {
                CharacterName = "Xaber",
                PositionName = "test item name",
                Price = new PoePrice("chaos", 5.5f),
                Timestamp = DateTime.Now
            };
            ProcessTrade(fake);
        }

        private void ApplyConfig(PoeTradeMonitorConfig config)
        {
            GrowUpwards = config.GrowUpwards;
            NumberOfNegotiationsToExpandByDefault = config.NumberOfNegotiationsToExpandByDefault;

            if (config.OverlaySize.Height <= 0 || config.OverlaySize.Width <= 0)
            {
                IsLocked = false;
                config.OverlaySize = MinSize;
            }
            Width = config.OverlaySize.Width;
            Height = config.OverlaySize.Height;

            if (config.OverlayLocation.X <= 1 && config.OverlayLocation.Y <= 1)
            {
                IsLocked = false;
                config.OverlayLocation = new Point(Width / 2, Height / 2);
            }
            Left = config.OverlayLocation.X;
            Top = config.OverlayLocation.Y;

            if (config.OverlayOpacity <= 0.01)
            {
                IsLocked = false;
                config.OverlayOpacity = 1;
            }
            Opacity = config.OverlayOpacity;
        }

        private void LockWindowCommandExecuted()
        {
            var config = configProvider.ActualConfig;
            config.OverlayLocation = new Point(Left, Top);
            config.GrowUpwards = GrowUpwards;
            config.NumberOfNegotiationsToExpandByDefault = NumberOfNegotiationsToExpandByDefault;
            config.OverlaySize = new Size(Width, Height);
            config.OverlayOpacity = Opacity;
            configProvider.Save(config);
            IsLocked = true;
        }

        private void UnlockWindowCommandExecuted()
        {
            IsLocked = false;
        }

        private class NegotiationClassController : INegotiationCloseController
        {
            private readonly INegotiationViewModel negotiation;
            private readonly IReactiveList<INegotiationViewModel> owner;

            public NegotiationClassController(IReactiveList<INegotiationViewModel> owner,
                INegotiationViewModel negotiation)
            {
                this.owner = owner;
                this.negotiation = negotiation;
            }

            public void Close()
            {
                owner.Remove(negotiation);
            }
        }
    }
}