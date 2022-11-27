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
    public bool IsVisible { get; set; }
    
    public bool IsInEditMode { get; set; }
    
    public WinRect ProjectionBounds { get; set; }

    public WinRect SelectionProjected { get; set; }

    public WinPoint MousePositionProjected { get; set; }

    bool IsBoxSelectionEnabled { get; set; }
    
    IObservable<WinRect> StartSelection(bool supportBoxSelection = true);
}

public sealed class SelectionAdorner : DisposableReactiveObject, ISelectionAdorner
{
    private static readonly Binder<SelectionAdorner> Binder = new();

    static SelectionAdorner()
    {
    }

    public SelectionAdorner()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsVisible { get; set; }
    
    public bool IsInEditMode { get; set; }

    public WinRect ProjectionBounds { get; set; }

    public WinRect SelectionProjected { get; set; }

    public WinPoint MousePositionProjected { get; set; }

    public bool IsBoxSelectionEnabled { get; set; }
    
    public IObservable<WinRect> StartSelection(bool supportBoxSelection = true)
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

            this.WhenAnyValue(x => x.SelectionProjected)
                .Skip(1)
                .TakeUntil(this.WhenAnyValue(x => x.IsInEditMode).Where(x => x == false))
                .Subscribe(observer)
                .AddTo(anchors);

            return anchors;
        });
    }
}