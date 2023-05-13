using System;
using System.Drawing;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Native;

public abstract class WindowContainerBase<T> : DisposableReactiveObject where T : IWindowViewModel
{
    private static readonly Binder<WindowContainerBase<T>> Binder = new();
    private const int MIN_THUMB_SIZE = 8;
    private const int MAX_THUMB_SIZE = 30;
    
    static WindowContainerBase()
    {
        Binder
            .BindIf(x => x.Content != null && x.Content.Dpi.IsEmpty == false, x => (double)x.Content.MinSize.Width / x.Content.Dpi.X)
            .Else(x => 0d)
            .To(x => x.MinWidth);

        Binder
            .BindIf(x => x.Content != null && x.Content.Dpi.IsEmpty == false, x => (double)x.Content.MaxSize.Width / x.Content.Dpi.X)
            .Else(x => double.NaN)
            .To(x => x.MaxWidth);

        Binder
            .BindIf(x => x.Content != null && x.Content.Dpi.IsEmpty == false, x => (double)x.Content.MinSize.Height / x.Content.Dpi.Y)
            .Else(x => 0d)
            .To(x => x.MinHeight);

        Binder
            .BindIf(x => x.Content != null && x.Content.Dpi.IsEmpty == false, x => (double)x.Content.MaxSize.Height / x.Content.Dpi.Y)
            .Else(x => double.NaN)
            .To(x => x.MaxHeight);

        Binder
            .Bind(x => CalculateThumbSize(x.NativeBounds))
            .To(x => x.ResizeThumbSize);
    }

    protected WindowContainerBase(IFluentLog logger)
    {
        Log = logger;
        Binder.Attach(this).AddTo(Anchors);
    }

    public T Content { get; set; }
    
    public DataTemplate ContentTemplate { get; set; }
    
    public bool IsFocusable { get; protected set; }
    
    public double MinWidth { get; [UsedImplicitly] private set; }
    public double MaxWidth { get; [UsedImplicitly] private set; }
    public double MinHeight { get; [UsedImplicitly] private set; }
    public double MaxHeight { get; [UsedImplicitly] private set; }
    public double ResizeThumbSize { get; [UsedImplicitly] protected set; }
    public bool ShowWireframes { get; set; }
    public double GlassFrameThickness { get; protected set; } = 1;
    public double ResizeBorderThickness { get; protected set; } = 1;
    public Rectangle NativeBounds { get; set; }
    
    protected IFluentLog Log { get; }

    private static double CalculateThumbSize(Rectangle nativeBounds)
    {
        var minSide = Math.Min(nativeBounds.Width, nativeBounds.Height);
        return (minSide / 25d).EnsureInRange(MIN_THUMB_SIZE, MAX_THUMB_SIZE);
    }

    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        builder.AppendParameter(nameof(Content), Content);
    }
}