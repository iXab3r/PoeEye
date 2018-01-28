﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeBud.Config;
using PoeEye.Config;
using PoeShared;
using PoeShared.Modularity;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using PoeShared.UI;
using Prism.Mvvm;
using ReactiveUI;
using ReactiveUI.Legacy;
using Squirrel;

namespace PoeEye.PoeTrade.Updater
{
    internal sealed class ApplicationUpdaterViewModel : DisposableReactiveObject
    {
        private readonly IApplicationUpdaterModel updaterModel;
        private readonly ReactiveCommand<Unit> checkForUpdatesCommand;
        private readonly ReactiveCommand<Unit> restartCommand;

        private bool isBusy;
        private bool isOpen;
        private string error = string.Empty;

        public ApplicationUpdaterViewModel(
            [NotNull] IApplicationUpdaterModel updaterModel,
            [NotNull] IConfigProvider<PoeEyeUpdateSettingsConfig> configProvider,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(updaterModel, nameof(updaterModel));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(configProvider, nameof(configProvider));

            updaterModel.WhenAnyValue(x => x.MostRecentVersion)
                .Subscribe(() => this.RaisePropertyChanged(nameof(MostRecentVersion)))
                .AddTo(Anchors);

            this.updaterModel = updaterModel;
            checkForUpdatesCommand = ReactiveUI.Legacy.ReactiveCommand
                .CreateAsyncTask(x => CheckForUpdatesCommandExecuted(), uiScheduler);

            checkForUpdatesCommand
                .ThrownExceptions
                .Subscribe(ex => Error = $"Update error: {ex.Message}")
                .AddTo(Anchors);

            updaterModel
                .WhenAnyValue(x => x.MostRecentVersion)
                .ObserveOn(uiScheduler)
                .Subscribe(() => this.RaisePropertyChanged(nameof(MostRecentVersion)), Log.HandleUiException)
                .AddTo(Anchors);
            
            restartCommand = ReactiveUI.Legacy.ReactiveCommand
                .CreateAsyncTask(x => RestartCommandExecuted(), uiScheduler);

            restartCommand
                .ThrownExceptions
                .Subscribe(ex => Error = $"Restart error: {ex.Message}")
                .AddTo(Anchors);
            
            configProvider
                .ListenTo(x => x.AutoUpdateTimeout)
                .WithPrevious((prev, curr) => new { prev, curr })
                .Do(timeout => Log.Instance.Debug($"[ApplicationUpdaterViewModel] AutoUpdate timout changed: {timeout.prev} => {timeout.curr}"))
                .Select(timeout => timeout.curr <= TimeSpan.Zero ? Observable.Never<long>() : Observable.Timer(DateTimeOffset.MinValue, timeout.curr, bgScheduler))
                .Switch()
                .ObserveOn(uiScheduler)
                .Subscribe(() => checkForUpdatesCommand.Execute(this), Log.HandleException)
                .AddTo(Anchors);
        }

        public ICommand CheckForUpdatesCommand => checkForUpdatesCommand;

        public ICommand RestartCommand => restartCommand;

        public string Error
        {
            get { return error; }
            set { this.RaiseAndSetIfChanged(ref error, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { this.RaiseAndSetIfChanged(ref isBusy, value); }
        }

        public bool IsOpen
        {
            get { return isOpen; }
            set { this.RaiseAndSetIfChanged(ref isOpen, value); }
        }

        public Version MostRecentVersion => updaterModel.MostRecentVersion;

        private async Task CheckForUpdatesCommandExecuted()
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel] Update check requested");
            IsBusy = true;
            Error = string.Empty;

            // delaying update so the user could see the progressring
            await Task.Delay(UiConstants.ArtificialLongDelay);

            try
            {
                IsOpen = await updaterModel.CheckForUpdates();
            }
            catch (Exception ex)
            {
                Log.HandleUiException(ex);
                IsOpen = true;
                Error = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
        
        private async Task RestartCommandExecuted()
        {
            Log.Instance.Debug($"[ApplicationUpdaterViewModel] Restart application requested");
            Error = string.Empty;

            try
            {
                await updaterModel.RestartApplication();
            }
            catch (Exception ex)
            {
                Log.HandleUiException(ex);
                Error = ex.Message;
            }
        }
    }
}
