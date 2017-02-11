using System;
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
using PoeShared;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Mvvm;
using ReactiveUI;
using Squirrel;

namespace PoeEye.PoeTrade.Updater
{
    internal sealed class ApplicationUpdaterViewModel : DisposableReactiveObject
    {
        private static readonly TimeSpan ArtificialDelay = TimeSpan.FromSeconds(5);

        private readonly ApplicationUpdaterModel updaterModel;
        private readonly ReactiveCommand<Unit> checkForUpdatesCommand;
        private readonly ReactiveCommand<object> restartCommand;

        private bool isBusy;
        private bool isOpen;
        private string error = string.Empty;

        public ApplicationUpdaterViewModel(
            [NotNull] ApplicationUpdaterModel updaterModel,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler)
        {
            Guard.ArgumentNotNull(() => updaterModel);
            Guard.ArgumentNotNull(() => uiScheduler);
            Guard.ArgumentNotNull(() => bgScheduler);

            updaterModel.WhenAnyValue(x => x.MostRecentVersion)
                .Subscribe(() => this.RaisePropertyChanged(nameof(MostRecentVersion)))
                .AddTo(Anchors);

            this.updaterModel = updaterModel;
            checkForUpdatesCommand = ReactiveCommand
                .CreateAsyncTask(x => CheckForUpdatesCommandExecuted(), uiScheduler);

            checkForUpdatesCommand
                .ThrownExceptions
                .Subscribe(ex => Error = $"Update error: {ex.Message}")
                .AddTo(Anchors);

            restartCommand = ReactiveCommand.Create();
            restartCommand.Subscribe(updaterModel.RestartApplication).AddTo(Anchors);
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
            await Task.Delay(ArtificialDelay);

            try
            {
                await updaterModel.CheckForUpdates();
                IsOpen = true;
            }
            catch (Exception ex)
            {
                Log.HandleException(ex);
                IsOpen = true;
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}