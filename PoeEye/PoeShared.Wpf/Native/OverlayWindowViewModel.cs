using System;
using System.Drawing;
using System.Windows;
using JetBrains.Annotations;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Native;

internal sealed class OverlayWindowViewModel : DisposableReactiveObject
{
    private const int MIN_THUMB_SIZE = 8;
    private const int MAX_THUMB_SIZE = 30;
    private static readonly Binder<OverlayWindowViewModel> Binder = new();

    static OverlayWindowViewModel()
    {
        Binder
            .BindIf(x => x.Content != null && !x.Content.IsLocked && x.Content.IsLoaded, x => 15d)
            .Else(x => 0d)
            .To((x, v) =>
            {
                x.Log.Debug(() => $"Setting {nameof(x.GlassFrameThickness)} to {v}, content native bounds: {x.Content?.NativeBounds}");
                x.GlassFrameThickness = v;
            });

        Binder
            .BindIf(x => x.Content != null && !x.Content.IsLocked && x.Content.IsLoaded, x => x.ResizeThumbSize / 2)
            .Else(x => 0d)
            .To((x, v) =>
            {
                x.Log.Debug(() => $"Setting {nameof(x.ResizeBorderThickness)} to {v}");
                x.ResizeBorderThickness = v;
            });

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

    public OverlayWindowViewModel(IFluentLog logger)
    {
        Log = logger;
        Binder.Attach(this).AddTo(Anchors);
    }

    private IFluentLog Log { get; }

    public bool ShowWireframes { get; set; }

    public double MinWidth { get; [UsedImplicitly] private set; }
    public double MaxWidth { get; [UsedImplicitly] private set; }
    public double MinHeight { get; [UsedImplicitly] private set; }
    public double MaxHeight { get; [UsedImplicitly] private set; }

    public Rectangle NativeBounds { get; set; }

    public double ResizeBorderThickness { get; private set; }

    public double ResizeThumbSize { get; [UsedImplicitly] private set; }

    public double GlassFrameThickness { get; private set; }

    public IOverlayViewModel Content { get; set; }

    public DataTemplate ContentTemplate { get; set; }

    public override string ToString()
    {
        return $"{Content} {Content?.OverlayMode}";
    }

    private static double CalculateThumbSize(Rectangle nativeBounds)
    {
        var minSide = Math.Min(nativeBounds.Width, nativeBounds.Height);
        return (minSide / 25d).EnsureInRange(MIN_THUMB_SIZE, MAX_THUMB_SIZE);
    }
}