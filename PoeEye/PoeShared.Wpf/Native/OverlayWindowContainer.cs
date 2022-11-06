using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Native;

internal sealed class OverlayWindowContainer : WindowContainerBase<IOverlayViewModel>
{
    private static readonly Binder<OverlayWindowContainer> Binder = new();

    static OverlayWindowContainer()
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
    }

    public OverlayWindowContainer(IFluentLog logger) : base(logger)
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public override string ToString()
    {
        return $"{Content} {Content?.OverlayMode}";
    }
}