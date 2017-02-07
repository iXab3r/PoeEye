using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Gma.System.MouseKeyHook;
using Guards;
using JetBrains.Annotations;
using Microsoft.Practices.Unity;
using PoeOracle.Models;
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

        [NotNull] private readonly IOverlayWindowController controller;

        private readonly ISuggestionsDataSource dataSource;
        private readonly DelegateCommand gotoGamepediaCommand;

        [NotNull] private readonly IExternalUriOpener uriOpener;

        private bool isFocused;

        private bool isVisible;
        private Point location;

        private double oracleActualWidth;

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

            this.WhenAnyValue(x => x.OracleActualWidth)
                .Where(x => IsVisible)
                .Subscribe(SnapToOverlayCenter)
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

        public IReactiveList<IOracleSuggestionViewModel> ItemsToShow => dataSource.Items;

        public ICommand GotoGamepediaCommand => gotoGamepediaCommand;

        public double OracleActualWidth
        {
            get { return oracleActualWidth; }
            set { this.RaiseAndSetIfChanged(ref oracleActualWidth, value); }
        }

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
            IsVisible = true;
            IsFocused = true;

            controller.Activate();
        }

        private void SnapToMouse()
        {
            var mousePosition = Control.MousePosition;
            Location = new Point(mousePosition.X, mousePosition.Y);
        }

        private void SnapToOverlayCenter()
        {
            var overlayLocation = controller.Location;
            var overlaySize = controller.Size;
            var oracleSize = new Size(OracleActualWidth, 0);

            var top = overlayLocation.Y + overlaySize.Height / 2;
            var left = overlayLocation.X + overlaySize.Width / 2;
            if (!double.IsNaN(oracleSize.Width) && !double.IsInfinity(oracleSize.Width) && !oracleSize.IsEmpty)
            {
                left -= oracleSize.Width / 2;
            }

            Location = new Point(left, top);
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