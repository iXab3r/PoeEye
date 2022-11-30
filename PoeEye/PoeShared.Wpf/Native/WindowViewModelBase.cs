﻿using System;
using System.Drawing;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicData.Binding;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;
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
        Binder.BindAction(x => x.Log.Info($"Title updated to {x.Title}"));
    }

    protected WindowViewModelBase()
    {
        Log = GetType().PrepareLogger().WithSuffix(Id).WithSuffix(ToString);
        Title = GetType().ToString();
        Binder.Attach(this).AddTo(Anchors);
        uiDispatcher = Dispatcher.CurrentDispatcher;
        
        this.WhenValueChanged(x => x.OverlayWindow, false)
            .Take(1)
            .Select(x => x.WhenLoaded())
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
        
        
        dpi = this.WhenAnyValue(x => x.OverlayWindow).Select(x => x == null ? Observable.Return(new PointF(1, 1)) : x.Observe(ConstantAspectRatioWindow.DpiProperty).Select(_ => OverlayWindow.Dpi))
            .Switch()
            .Do(x => Log.Debug(() => $"DPI updated to {x}"))
            .ToProperty(this, x => x.Dpi)
            .AddTo(Anchors);
        
        

        WhenKeyDown = this.WhenAnyValue(x => x.OverlayWindow)
            .Select(window => window != null ? Observable.FromEventPattern<KeyEventHandler, KeyEventArgs>(h => window.KeyDown += h, h => window.KeyDown -= h).Select(x => x) : Observable.Empty<EventPattern<KeyEventArgs>>())
            .Switch();
        
        this.WhenAnyValue(x => x.OverlayWindow)
            .SwitchIfNotDefault(x => x.Observe(ConstantAspectRatioWindow.NativeBoundsProperty, y => y.NativeBounds).Select(y => new { Window = x, ActualBounds = y }))
            .Subscribe(x =>
            {
                // always on UI thread
                Log.Debug(() => $"Updating {nameof(NativeBounds)}: {NativeBounds} => {x.ActualBounds}");
                NativeBounds = x.ActualBounds;
                Log.Debug(() => $"Updated {nameof(NativeBounds)}: {NativeBounds} => {x.ActualBounds}");
            })
            .AddTo(Anchors); 

        this.WhenAnyValue(x => x.OverlayWindow)
            .SwitchIfNotDefault(x => this.WhenAnyValue(y => y.NativeBounds)
                .Select(y => new { Window = x, DesiredBounds = y })
                .ObserveOnIfNeeded(x.Dispatcher))
            .Subscribe(x =>
            {
                // always on UI thread, possible recursive assignment
                var overlayBounds = x.Window.NativeBounds;
                Log.Debug(() => $"Updating Overlay {nameof(NativeBounds)}: {overlayBounds} => {x}");
                x.Window.NativeBounds = x.DesiredBounds;
                Log.Debug(() => $"Updated Overlay {nameof(NativeBounds)}: {overlayBounds} => {x}");
            })
            .AddTo(Anchors);
    }

    protected IFluentLog Log { get; }

    protected IObservable<Unit> WhenLoaded => whenLoaded;
    
    public TransparentWindow OverlayWindow { get; private set; }
    
    public IObservable<EventPattern<KeyEventArgs>> WhenKeyDown { get; }

    public bool IsVisible { get; set; } = true;

    public PointF Dpi => dpi.Value;

    public Rectangle NativeBounds { get; set; }

    public Size MinSize { get; set; } = new Size(0, 0);

    public Size MaxSize { get; set; } = new Size(Int16.MaxValue, Int16.MaxValue);
    
    public bool IsLoaded { get; private set; }

    public SizeToContent SizeToContent { get; protected set; } = SizeToContent.Manual;

    public string Id { get; } = $"Overlay#{Interlocked.Increment(ref GlobalWindowId)}";

    public string Title { get; protected set; }
    
    public Size DefaultSize { get; set; }

    public string OverlayDescription => $"{(OverlayWindow == null ? "NOWINDOW" : OverlayWindow.Name)}";
    
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

    public void SetOverlayWindow(TransparentWindow owner)
    {
        Guard.ArgumentNotNull(owner, nameof(owner));

        if (OverlayWindow != null)
        {
            Log.Info(() => $"Re-assigning overlay window: {OverlayWindow} => {owner}");
        }
        else
        {
            Log.Info(() => $"Assigning overlay window: {owner}");
        }

        Log.Info(() => $"Syncing window parameters with view model");
        owner.NativeBounds = NativeBounds;
        OverlayWindow = owner;
        Log.Info(() => $"Overlay window is assigned: {OverlayWindow}");
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
}