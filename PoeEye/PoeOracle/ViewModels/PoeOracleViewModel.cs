using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeOracle.Models;
using PoeShared;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.Scaffolding;
using Prism.Commands;
using ReactiveUI;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using KeyEventHandler = System.Windows.Forms.KeyEventHandler;

namespace PoeOracle.ViewModels
{
    internal sealed class PoeOracleViewModel : DisposableReactiveObject, IOverlayViewModel
    {
        private static readonly TimeSpan DefaultQueryThrottle = TimeSpan.FromMilliseconds(200);

        [NotNull]
        private readonly IOverlayWindowController controller;
        private readonly ISuggestionsDataSource dataSource;
        [NotNull]
        private readonly IExternalUriOpener uriOpener;
        private readonly DelegateCommand gotoGamepediaCommand;
        private bool isFocused;

        private bool isVisible;
        private Point location;

        private string query;

        private Size size = new Size(double.NaN, double.NaN);

        public PoeOracleViewModel(
            [NotNull] IKeyboardMouseEvents keyboardMouseEvents,
            [NotNull] IOverlayWindowController controller,
            [NotNull] ISuggestionsDataSource dataSource,
            [NotNull] IExternalUriOpener uriOpener,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(() => keyboardMouseEvents);
            Guard.ArgumentNotNull(() => controller);
            Guard.ArgumentNotNull(() => dataSource);
            Guard.ArgumentNotNull(() => uriOpener);
            Guard.ArgumentNotNull(() => bgScheduler);
            Guard.ArgumentNotNull(() => uiScheduler);

            this.controller = controller;
            this.dataSource = dataSource;
            this.uriOpener = uriOpener;
            dataSource.AddTo(Anchors);

            gotoGamepediaCommand = new DelegateCommand(GotoGamepediaCommandExecuted, CanExecuteGotoGamepediaCommand);
            this.WhenAnyValue(x => x.Query).Subscribe(gotoGamepediaCommand.RaiseCanExecuteChanged).AddTo(Anchors);

            Observable
                .FromEventPattern<KeyEventHandler, KeyEventArgs>(
                    h => keyboardMouseEvents.KeyDown += h,
                    h => keyboardMouseEvents.KeyDown -= h)
                .Select(x => x.EventArgs)
                .Where(x => controller.IsVisible)
                .Subscribe(ProcessKeyDown)
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.Query)
                .Throttle(DefaultQueryThrottle)
                .DistinctUntilChanged()
                .Subscribe(x => dataSource.Query = x)
                .AddTo(Anchors);

            dataSource.WhenAnyValue(x => x.IsBusy)
                .DistinctUntilChanged()
                .Throttle(DefaultQueryThrottle, bgScheduler)
                .ObserveOn(uiScheduler)
                .Subscribe(x => this.RaisePropertyChanged(nameof(IsBusy)))
                .AddTo(Anchors);

            this.WhenAnyValue(x => x.IsFocused)
                .Where(isFocused => isFocused == false)
                .Subscribe(() => Hide(false))
                .AddTo(Anchors);
        }

        public bool IsFocused
        {
            get { return isFocused; }
            set { this.RaiseAndSetIfChanged(ref isFocused, value); }
        }

        public bool IsVisible
        {
            get { return isVisible; }
            set { this.RaiseAndSetIfChanged(ref isVisible, value); }
        }

        public string Query
        {
            get { return query; }
            set { this.RaiseAndSetIfChanged(ref query, value); }
        }

        public bool IsBusy => dataSource.IsBusy;

        public Point Location
        {
            get { return location; }
            set { this.RaiseAndSetIfChanged(ref location, value); }
        }

        public Size Size
        {
            get { return size; }
            set { this.RaiseAndSetIfChanged(ref size, value); }
        }

        public IReactiveList<IOracleSuggestionViewModel> ItemsToShow => dataSource.Items;

        public ICommand GotoGamepediaCommand => gotoGamepediaCommand;

        private void ProcessKeyDown(KeyEventArgs keyEventArgs)
        {
            if (new KeyGesture(Key.Escape).MatchesHotkey(keyEventArgs) && IsVisible)
            {
                keyEventArgs.Handled = true;
                Hide(true);
            }
            else if (new KeyGesture(Key.T, ModifierKeys.Control).MatchesHotkey(keyEventArgs))
            {
                keyEventArgs.Handled = true;
                Show();
            }
        }

        private void GotoGamepediaCommandExecuted()
        {
            var wikiUri = $"http://pathofexile.gamepedia.com/index.php?search={query}";
            uriOpener.Request(wikiUri);
        }

        private bool CanExecuteGotoGamepediaCommand()
        {
            return !string.IsNullOrWhiteSpace(query);
        }

        private void Show()
        {
            var mousePosition = System.Windows.Forms.Control.MousePosition;
            Location = new Point(mousePosition.X, mousePosition.Y);

            IsVisible = true;
            IsFocused = true;

            controller.Activate();
        }

        private void Hide(bool restoreLastActiveWindow)
        {
            IsVisible = false;
            Query = null;

            if (restoreLastActiveWindow)
            {
                controller.ActivateLastActiveWindow();
            }
        }
    }
}