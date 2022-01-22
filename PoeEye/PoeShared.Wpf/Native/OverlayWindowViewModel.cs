using System.Windows;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Native;

internal sealed class OverlayWindowViewModel : DisposableReactiveObject
{
    private static readonly Binder<OverlayWindowViewModel> Binder = new();

    static OverlayWindowViewModel()
    {
        Binder
            .BindIf(x => x.Content != null && !x.Content.IsLocked && x.Content.IsLoaded, x => 15d)
            .Else(x => 0d)
            .To((x, v) =>
            {
                x.Log.Debug(() => $"Setting {nameof(x.GlassFrameThickness)} to {v}, content native bounds: {x.Content.NativeBounds}");
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
    }

    public OverlayWindowViewModel(IFluentLog logger)
    {
        Log = logger;
        Binder.Attach(this).AddTo(Anchors);
    }
        
    private IFluentLog Log { get; }

    public bool ShowWireframes { get; set; }

    public double ResizeBorderThickness { get; private set; }

    public double ResizeThumbSize { get; set; }

    public double GlassFrameThickness { get; private set; }

    public IOverlayViewModel Content { get; set; }

    public DataTemplate ContentTemplate { get; set; }

    public override string ToString()
    {
        return $"{Content} {Content?.OverlayMode}";
    }
}