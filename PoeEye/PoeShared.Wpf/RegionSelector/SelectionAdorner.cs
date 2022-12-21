using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Media;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.RegionSelector;

public interface ISelectionAdorner : IDisposableReactiveObject
{
    bool IsVisible { get; set; }
    
    bool IsInEditMode { get; set; }
    
    bool ShowBackground { get; set; }
    
    bool ShowCrosshair { get; set; } 
    
    bool IsBoxSelectionEnabled { get; set; }
    
    /// <summary>
    /// "Virtual" bounds of selection region which will be used for mapping
    /// </summary>
    WinRect ProjectionBounds { get; set; }
    
    /// <summary>
    /// Selection region mapped to virtual bounds
    /// </summary>
    WinRect SelectionProjected { get; set; }

    /// <summary>
    /// Mouse position mapped to virtual bounds
    /// </summary>
    WinPoint MousePositionProjected { get; set; }

    /// <summary>
    /// Mouse position relative to top-left corner 
    /// </summary>
    WinPoint MousePosition { get; }

    /// <summary>
    /// Selection relative to top-left corner
    /// </summary>
    WinRect Selection { get; }

    IObservable<WinRect> SelectRegion();
    
    IObservable<WinPoint> SelectPoint();
    
    IObservable<WinRect> SelectVirtualRegion();
    
    IObservable<WinPoint> SelectVirtualPoint();
}

public sealed class SelectionAdorner : DisposableReactiveObject, ISelectionAdorner
{
    private static readonly Binder<SelectionAdorner> Binder = new();

    static SelectionAdorner()
    {
        Binder.BindIf(x => !x.SelectionProjected.IsEmpty, x => x.SelectionProjected.OffsetBy(x.ProjectionBounds.Location.Negate()))
            .Else(x => default)
            .To(x => x.Selection);
        Binder
            .BindIf(x => !x.MousePositionProjected.IsEmpty, x  => x.MousePositionProjected.OffsetBy(x.ProjectionBounds.Location.Negate()))
            .Else(x => default)
            .To(x => x.MousePosition);
    }

    public SelectionAdorner()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsVisible { get; set; }
    
    public bool IsInEditMode { get; set; }
    
    public bool ShowBackground { get; set; } = true;
    
    public bool ShowCrosshair { get; set; } = true;

    public WinRect ProjectionBounds { get; set; }

    public WinPoint MousePosition { get; [UsedImplicitly] private set; }
    
    public WinRect Selection { get; [UsedImplicitly] private set; }

    public WinRect SelectionProjected { get; set; }

    public WinPoint MousePositionProjected { get; set; }

    public bool IsBoxSelectionEnabled { get; set; }
    
    public IObservable<WinRect> StartSelection(bool supportBoxSelection = true)
    {
        return StartSelection(true, supportBoxSelection);
    }

    public IObservable<WinRect> StartSelection(bool isVirtual, bool supportBoxSelection)
    {
        return Observable.Create<WinRect>(observer =>
        {
            var anchors = new CompositeDisposable();

            IsVisible = true;
            IsInEditMode = true;
            IsBoxSelectionEnabled = supportBoxSelection;
            Disposable.Create(() =>
            {
                IsInEditMode = false;
                IsVisible = false;
                SelectionProjected = WinRect.Empty;
            }).AddTo(anchors);

            var selectionSource = isVirtual ? this.WhenAnyValue(x => x.SelectionProjected) : this.WhenAnyValue(x => x.Selection);
            selectionSource
                .Skip(1)
                .TakeUntil(this.WhenAnyValue(x => x.IsInEditMode).Where(x => x == false))
                .Subscribe(observer)
                .AddTo(anchors);

            return anchors;
        });
    }

    public IObservable<WinRect> SelectRegion()
    {
        return StartSelection(isVirtual: false, supportBoxSelection: true);
    }

    public IObservable<WinPoint> SelectPoint()
    {
        return StartSelection(isVirtual: false, supportBoxSelection: false).Select(x => x.Location);
    }

    public IObservable<WinRect> SelectVirtualRegion()
    {
        return StartSelection(isVirtual: true, supportBoxSelection: true);
    }

    public IObservable<WinPoint> SelectVirtualPoint()
    {
        return StartSelection(isVirtual: true, supportBoxSelection: false).Select(x => x.Location);
    }
}