﻿using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.PoeControlPanel.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using Unity.Attributes;

namespace PoeShared.PoeControlPanel.ViewModels
{
    internal sealed class PoeControlPanelViewModel : OverlayViewModelBase
    {
        private readonly IConfigProvider<PoeControlPanelConfig> configProvider;
        private readonly IOverlayWindowController controller;

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
            IsUnlockable = true;
            Title = "Control panel";

            UnlockAllWindowsCommand = new DelegateCommand(UnlockAllWindowsCommandExecuted);
            LockAllWindowsCommand = new DelegateCommand(LockAllWindowsCommandExecuted);

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

        public ICommand UnlockAllWindowsCommand { get; }
        
        public ICommand LockAllWindowsCommand { get; }

        private void ApplyConfig(PoeControlPanelConfig config)
        {
            base.ApplyConfig(config);
        }

        protected override void LockWindowCommandExecuted()
        {
            base.LockWindowCommandExecuted();

            var config = configProvider.ActualConfig;
            SavePropertiesToConfig(config);
            configProvider.Save(config);
        }

        private void UnlockAllWindowsCommandExecuted()
        {
            foreach (var overlayViewModel in controller.GetChildren()
                                                       .Where(x => x.UnlockWindowCommand.CanExecute(null)))
            {
                overlayViewModel.UnlockWindowCommand.Execute(null);
            }
        }
        
        private void LockAllWindowsCommandExecuted()
        {
            foreach (var overlayViewModel in controller.GetChildren()
                                                       .Where(x => x.LockWindowCommand.CanExecute(null)))
            {
                overlayViewModel.LockWindowCommand.Execute(null);
            }
        }
    }
}