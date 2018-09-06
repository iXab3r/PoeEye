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
    internal sealed class PoeOracleViewModel : OverlayViewModelBase
    {
        private static readonly TimeSpan DefaultQueryThrottle = TimeSpan.FromMilliseconds(200);

        private readonly IOverlayWindowController controller;

        private readonly ISuggestionsDataSource dataSource;
        private readonly DelegateCommand gotoGamepediaCommand;

        private readonly IExternalUriOpener uriOpener;

        private bool isFocused;

        private bool isVisible;

        private string query;
        private IActivationController activationController;

        public PoeOracleViewModel(
            [NotNull] IKeyboardEventsSource keyboardMouseEvents,
            [NotNull] IOverlayWindowController controller,
            [NotNull] ISuggestionsDataSource dataSource,
            [NotNull] IExternalUriOpener uriOpener,
            [NotNull] [Dependency(WellKnownSchedulers.Background)] IScheduler bgScheduler,
            [NotNull] [Dependency(WellKnownSchedulers.UI)] IScheduler uiScheduler)
        {
            Guard.ArgumentNotNull(keyboardMouseEvents, nameof(keyboardMouseEvents));
            Guard.ArgumentNotNull(controller, nameof(controller));
            Guard.ArgumentNotNull(dataSource, nameof(dataSource));
            Guard.ArgumentNotNull(uriOpener, nameof(uriOpener));
            Guard.ArgumentNotNull(bgScheduler, nameof(bgScheduler));
            Guard.ArgumentNotNull(uiScheduler, nameof(uiScheduler));

            SizeToContent = SizeToContent.WidthAndHeight;
            MinSize = new Size(700, 50);
            OverlayMode = OverlayMode.Layered;

            this.controller = controller;
            this.dataSource = dataSource;
            this.uriOpener = uriOpener;
            dataSource.AddTo(Anchors);

            gotoGamepediaCommand = new DelegateCommand(GotoGamepediaCommandExecuted, CanExecuteGotoGamepediaCommand);
            this.WhenAnyValue(x => x.Query).Subscribe(gotoGamepediaCommand.RaiseCanExecuteChanged).AddTo(Anchors);

            keyboardMouseEvents
                .WhenKeyDown
                .Where(x => controller.IsVisible)
                .ObserveOn(uiScheduler)
                .Subscribe(ProcessKeyDown, Log.HandleException)
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

            this.WhenAnyValue(x => x.IsVisible)
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

        public override void SetActivationController(IActivationController controller)
        {
            Guard.ArgumentNotNull(controller, nameof(controller));
            activationController = controller;
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

            activationController?.Activate();
        }

        private void SnapToOverlayCenter()
        {
            var top = SystemParameters.PrimaryScreenHeight / 2 - ActualHeight / 2;
            var left = SystemParameters.PrimaryScreenWidth / 2 - ActualWidth / 2;

            Left = left;
            Top = top;
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
