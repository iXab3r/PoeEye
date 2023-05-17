using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicData.Binding;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.UI;
using PropertyBinder;
using ReactiveUI;
using Unity;
using Size = System.Drawing.Size;

namespace PoeShared.Native;

public abstract class WindowViewModelBase : DisposableReactiveObject, IWindowViewModel
{
    private static readonly Binder<WindowViewModelBase> Binder = new();
    private static long GlobalWindowId;

    private readonly ObservableAsPropertyHelper<PointF> dpi;
    private readonly ISubject<Unit> whenLoaded = new ReplaySubject<Unit>(1);
    private readonly Dispatcher uiDispatcher;

    static WindowViewModelBase()
    {
        Binder.BindAction(x => x.Log.Info(() => $"Title updated to {x.Title}"));

        Binder.BindIf(x => x.WindowController != null, x => x.TargetAspectRatio).To((x, v) => x.WindowController.Window.TargetAspectRatio = v);
        Binder.BindIf(x => x.WindowController != null, x => x.SizeToContent).To((x, v) => x.WindowController.Window.SizeToContent = v);
    }

    protected WindowViewModelBase()
    {
        Log = GetType().PrepareLogger().WithSuffix(Id).WithSuffix(ToString);
        Title = GetType().ToString();
        Binder.Attach(this).AddTo(Anchors);
        uiDispatcher = Dispatcher.CurrentDispatcher;

        this.WhenValueChanged(x => x.WindowController, false)
            .Take(1)
            .Select(x => x.WhenLoaded)
            .Switch()
            .SubscribeSafe(whenLoaded)
            .AddTo(Anchors);
        whenLoaded.SubscribeSafe(_ =>
        {
            if (IsLoaded)
            {
                Log.Warn("Window received multiple 'loaded' events");
                throw new ApplicationException($"Window has already been loaded: {this}");
            }

            Log.Debug("Window has been loaded, changing status");
            IsLoaded = true;
        }, Log.HandleUiException).AddTo(Anchors);


        dpi = this.WhenAnyValue(x => x.WindowController).Select(x => x == null ? Observable.Return(new PointF(1, 1)) : x.WhenAnyValue(y => y.Window.Dpi))
            .Switch()
            .Do(x => Log.Debug(() => $"DPI updated to {x}"))
            .ToProperty(this, x => x.Dpi)
            .AddTo(Anchors);

        WhenKeyDown = this.WhenAnyValue(x => x.WindowController).SwitchIfNotDefault(x => x.WhenKeyDown);
        WhenKeyUp = this.WhenAnyValue(x => x.WindowController).SwitchIfNotDefault(x => x.WhenKeyUp);
        WhenPreviewKeyDown = this.WhenAnyValue(x => x.WindowController).SwitchIfNotDefault(x => x.WhenPreviewKeyDown);
        WhenPreviewKeyUp = this.WhenAnyValue(x => x.WindowController).SwitchIfNotDefault(x => x.WhenPreviewKeyUp);

        this.WhenAnyValue(x => x.WindowController)
            .SwitchIfNotDefault(x => x.WhenAnyValue(y => y.Window.NativeBounds).Select(y => new {Window = x, ActualBounds = y}))
            .Subscribe(x =>
            {
                // always on UI thread
                Log.Debug(() => $"Updating {nameof(NativeBounds)}: {NativeBounds} => {x.ActualBounds}");
                NativeBounds = x.ActualBounds;
                Log.Debug(() => $"Updated {nameof(NativeBounds)}: {NativeBounds} => {x.ActualBounds}");
            })
            .AddTo(Anchors);

        this.WhenAnyValue(x => x.WindowController)
            .SwitchIfNotDefault(x => this.WhenAnyValue(y => y.NativeBounds)
                .Select(y => new {x.Window, DesiredBounds = y})
                .ObserveOnIfNeeded(x.Window.Dispatcher))
            .Subscribe(x =>
            {
                // always on UI thread, possible recursive assignment
                var overlayBounds = x.Window.NativeBounds;
                Log.Debug(() => $"Updating Overlay {nameof(NativeBounds)}: {overlayBounds} => {x}");
                x.Window.NativeBounds = x.DesiredBounds;
                Log.Debug(() => $"Updated Overlay {nameof(NativeBounds)}: {overlayBounds} => {x}");
            })
            .AddTo(Anchors);


        this.WhenAnyValue(y => y.WindowController)
            .CombineLatest(this.WhenAnyValue(x => x.ForegroundWindowTracker), (winController, tracker) => new {window = winController, tracker})
            .Select(x => SubscribeToActivations(x.window?.Window, x.tracker))
            .Switch()
            .DistinctUntilChanged()
            .Subscribe(x =>
            {
                Log.Debug($"Updating IsActive to {x}");
                IsActive = x;
                Log.Debug($"Updated IsActive to {x}");
            })
            .AddTo(Anchors);
    }

    protected IFluentLog Log { get; }

    protected IObservable<Unit> WhenLoaded => whenLoaded;

    public IWindowViewController WindowController { get; private set; }

    public IObservable<KeyEventArgs> WhenKeyUp { get; }

    public IObservable<KeyEventArgs> WhenKeyDown { get; }

    public IObservable<KeyEventArgs> WhenPreviewKeyDown { get; }

    public IObservable<KeyEventArgs> WhenPreviewKeyUp { get; }

    [Dependency] public IForegroundWindowTracker ForegroundWindowTracker { get; [UsedImplicitly] init; }

    public bool IsVisible { get; set; } = true;

    public PointF Dpi => dpi.Value;

    public Rectangle NativeBounds { get; set; }

    public Size MinSize { get; set; } = new Size(0, 0);

    public Size MaxSize { get; set; } = new Size(Int16.MaxValue, Int16.MaxValue);

    public bool IsLoaded { get; private set; }

    public bool IsActive { get; private set; }

    public SizeToContent SizeToContent { get; protected set; } = SizeToContent.Manual;

    public string Id { get; } = $"Overlay#{Interlocked.Increment(ref GlobalWindowId)}";

    public string Title { get; protected set; }

    public Size DefaultSize { get; set; }

    public string OverlayDescription => $"{(WindowController == null ? "NOWINDOW" : WindowController)}";

    public bool EnableHeader { get; set; } = true;

    public bool ShowInTaskbar { get; set; }

    public double? TargetAspectRatio { get; set; }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.Append("Window");
        builder.AppendParameter(nameof(Id), Id);
        builder.AppendParameter(nameof(NativeBounds), NativeBounds);
    }

    public void SetOverlayWindow(IWindowViewController owner)
    {
        Guard.ArgumentNotNull(owner, nameof(owner));

        if (WindowController != null)
        {
            Log.Info(() => $"Re-assigning overlay window: {WindowController} => {owner}");
        }
        else
        {
            Log.Info(() => $"Assigning overlay window: {owner}");
        }

        Log.Info(() => $"Syncing window parameters with view model");
        owner.Window.NativeBounds = NativeBounds;
        WindowController = owner;
        Log.Info(() => $"Overlay window is assigned: {WindowController}");
    }

    public DispatcherOperation BeginInvoke(Action dispatcherAction)
    {
        return uiDispatcher.BeginInvoke(() =>
        {
            try
            {
                dispatcherAction();
            }
            catch (Exception e)
            {
                Log.Warn($"Failed to execute operation on dispatcher", e);
            }
        });
    }

    private static IObservable<bool> SubscribeToActivations(ReactiveMetroWindow window, IForegroundWindowTracker foregroundWindowTracker)
    {
        if (window == null || foregroundWindowTracker == null)
        {
            return Observable.Return(false);
        }

        return Observable.Create<bool>(observer =>
        {
            var anchors = new CompositeDisposable();

            foregroundWindowTracker
                .WhenAnyValue(x => x.ForegroundWindow)
                .Select(x => window.WindowHandle == x.Handle)
                .Subscribe(observer)
                .AddTo(anchors);

            /* This is extremely unreliable under load
            var initialValue = window.IsActive;
            observer.OnNext(initialValue);
            Observable.FromEventPattern<EventHandler, EventArgs>(h => window.Activated += h, h => window.Activated -= h)
                .Subscribe(() => observer.OnNext(true))
                .AddTo(anchors);
            
            Observable.FromEventPattern<EventHandler, EventArgs>(h => window.Deactivated += h, h => window.Deactivated -= h)
                .Subscribe(() => observer.OnNext(false))
                .AddTo(anchors);*/

            return anchors;
        });
    }
}