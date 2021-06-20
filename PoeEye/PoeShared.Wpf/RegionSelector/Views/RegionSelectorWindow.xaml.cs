using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using JetBrains.Annotations;
using log4net;
using PoeShared.Native;
using PoeShared.Prism;
using PoeShared.RegionSelector.ViewModels;
using PoeShared.Scaffolding;
using PoeShared.UI;
using ReactiveUI;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyEventHandler = System.Windows.Input.KeyEventHandler;

namespace PoeShared.RegionSelector.Views
{
    public partial class RegionSelectorWindow : IDisposable
    {
        private readonly IRegionSelectorViewModel viewModel;
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegionSelectorWindow));

        private readonly CompositeDisposable anchors = new CompositeDisposable();
        private readonly ISubject<string> closeWindowRequest = new Subject<string>();

        public RegionSelectorWindow(
            [NotNull] IFactory<IRegionSelectorViewModel, ICloseController<RegionSelectorResult>> viewModelFactory)
        {
            InitializeComponent();

            Disposable.Create(() => Log.Debug("RegionSelectorWindow disposed")).AddTo(anchors);
            Closed += OnClosed;
            Loaded += OnLoaded;

            CloseController = new ParameterizedCloseController<Window, RegionSelectorResult>(this,
                result =>
                {
                    var windowHandle = new WindowInteropHelper(this).Handle;
                    var state = new {IsVisible, Visibility, IsActive};
                    if (state.IsVisible && state.Visibility == Visibility.Visible)
                    {
                        Log.Debug($"[{windowHandle.ToHexadecimal()}] Closing RegionSelector window({state}), result - {result}");
                        Result = result;
                        Close();
                    }
                    else
                    {
                        Log.Debug($"[{windowHandle.ToHexadecimal()}] Ignoring Close request for RegionSelector window({state}) - already closed");
                    }
                });
            
            viewModel = viewModelFactory.Create(CloseController).AddTo(anchors);
            DataContext = viewModel;
        }

        public RegionSelectorResult Result { get; private set; }
        
        public ICloseController<RegionSelectorResult> CloseController { get; }

        public void Dispose()
        {
            anchors?.Dispose();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            Log.Debug($"[{windowHandle.ToHexadecimal()}] Window loaded");
            
            var workingArea = SystemInformation.VirtualScreen;
            Left = workingArea.Left;
            Top = workingArea.Top;
            Width = workingArea.Width;
            Height = workingArea.Height;

            if (!UnsafeNative.SetForegroundWindow(windowHandle))
            {
                Log.Warn($"[{windowHandle.ToHexadecimal()}] Failed to bring window to front");
            }

            Disposable.Create(() => closeWindowRequest.OnNext("window is disposed"))
                .AddTo(anchors);

            Observable.Merge(
                    Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(h => PreviewKeyDown += h, h => PreviewKeyDown -= h)
                        .Where(x => x.EventArgs.Key == Key.Escape)
                        .Select(x => "ESC pressed"),
                    Observable.FromEventPattern<RoutedEventHandler, RoutedEventArgs>(h => LostFocus += h, h => LostFocus -= h).Select(x => "window LostFocus"),
                    Observable.FromEventPattern<EventHandler, EventArgs>(h => Deactivated += h, h => Deactivated -= h).Select(x => "window Deactivated"))
                .SubscribeSafe(closeWindowRequest)
                .AddTo(anchors);

            closeWindowRequest
                .Take(1)
                .SubscribeSafe(reason => CloseController.Close(new RegionSelectorResult() { Reason = reason }), Log.HandleUiException)
                .AddTo(anchors);

            viewModel.WhenAnyValue(x => x.SelectionCandidate)
                .SubscribeSafe(
                    regionResult =>
                    {
                        if (regionResult == null || !regionResult.IsValid)
                        {
                            RegionCandidate.Visibility = Visibility.Collapsed;
                            return;
                        } 
                        
                        RegionCandidate.Visibility = Visibility.Visible;
                        var bounds = regionResult.Window.WindowBounds.ScaleToWpf();
                        var relative = this.PointFromScreen(bounds.Location);
                        Canvas.SetLeft(RegionCandidate, relative.X);
                        Canvas.SetTop(RegionCandidate, relative.Y);
                        RegionCandidate.Width = bounds.Width;
                        RegionCandidate.Height = bounds.Height;
                    }, Log.HandleUiException)
                .AddTo(anchors);
        }

        public void SelectScreenCoordinates()
        {
            viewModel
                .SelectScreenCoordinates()
                .SubscribeSafe(
                    result =>
                    {
                        CloseController.Close(result);
                    },
                    Log.HandleException,
                    () => { closeWindowRequest.OnNext("region selection cancelled"); })
                .AddTo(anchors);
        }
        
        public void SelectWindow(System.Drawing.Size minSelection)
        {
            viewModel
                .SelectWindow(minSelection)
                .SubscribeSafe(
                    result =>
                    {
                        CloseController.Close(result);
                    },
                    Log.HandleException,
                    () => { closeWindowRequest.OnNext("region selection cancelled"); })
                .AddTo(anchors);
        }
        
        private void OnClosed(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}