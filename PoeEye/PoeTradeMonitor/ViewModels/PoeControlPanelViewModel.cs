using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Anotar.Log4Net;
using DynamicData;
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
using PoeShared.StashApi.DataTypes;
using Prism.Commands;
using ReactiveUI;

namespace PoeEye.TradeMonitor.ViewModels
{
    internal sealed class PoeControlPanelViewModel : OverlayViewModelBase
    {
        private readonly IOverlayWindowController controller;
        private readonly IConfigProvider<PoeControlPanelConfig> configProvider;
        private float opacity;

        public PoeControlPanelViewModel(
            [NotNull] IOverlayWindowController controller,
            [NotNull] IConfigProvider<PoeControlPanelConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            this.controller = controller;
            this.configProvider = configProvider;
            Guard.ArgumentNotNull(controller, nameof(controller));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            MinSize = new Size(30, 30);
            MaxSize = new Size(300, 300);
            Top = 200;
            Left = 200;
            SizeToContent = SizeToContent.WidthAndHeight;

            LockWindowCommand = new DelegateCommand(LockWindowCommandExecuted);

            UnlockAllWindowsCommand = new DelegateCommand(UnlockAllWindowsCommandExecuted);

            WhenLoaded.Subscribe(
                    () =>
                    {
                        configProvider
                            .WhenChanged
                            .Subscribe(ApplyConfig)
                            .AddTo(Anchors);
                    })
                .AddTo(Anchors);
        }

        public float Opacity
        {
            get => opacity;
            set => this.RaiseAndSetIfChanged(ref opacity, value);
        }

        public ICommand LockWindowCommand { get; }

        public ICommand UnlockAllWindowsCommand { get; }

        private void ApplyConfig(PoeControlPanelConfig config)
        {
            if (config.OverlayOpacity <= 0.01)
            {
                IsLocked = false;
                config.OverlayOpacity = 1;
            }
            Opacity = config.OverlayOpacity;

            if (config.OverlayLocation.X <= 1 && config.OverlayLocation.Y <= 1)
            {
                IsLocked = false;
                config.OverlayLocation = new Point(Width / 2, Height / 2);
            }
            Left = config.OverlayLocation.X;
            Top = config.OverlayLocation.Y;
        }

        private void LockWindowCommandExecuted()
        {
            var config = configProvider.ActualConfig;
            config.OverlayLocation = new Point(Left, Top);
            config.OverlayOpacity = Opacity;
            configProvider.Save(config);
            IsLocked = true;
        }

        private void UnlockAllWindowsCommandExecuted()
        {
            //FIXME: These types should be provided via interface
            var knownTypes = new[]
            {
                typeof(PoeStashGridViewModel),
                typeof(PoeTradeMonitorViewModel),
                typeof(PoeControlPanelViewModel),
            };
            foreach (var overlayViewModel in controller.GetChilds())
            {
                if (!knownTypes.Contains(overlayViewModel.GetType()))
                {
                    continue;
                }
                overlayViewModel.IsLocked = false;
            }
        }
    }
}