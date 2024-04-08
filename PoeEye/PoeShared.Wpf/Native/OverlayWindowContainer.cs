using PoeShared.Logging;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Native;

public class OverlayWindowContainer<T> : WindowContainerBase<T>, IOverlayWindowContainer<T> where T : IOverlayViewModel
{
    private static readonly Binder<OverlayWindowContainer<T>> Binder = new();

    static OverlayWindowContainer()
    {
        Binder
            .BindIf(x => x.Content != null && !x.Content.IsLocked && x.Content.IsLoaded, x => 15d)
            .Else(x => 0d)
            .To((x, v) =>
            {
                x.Log.Debug($"Setting {nameof(x.GlassFrameThickness)} to {v}, content native bounds: {x.Content?.NativeBounds}");
                x.GlassFrameThickness = v;
            });

        Binder
            .BindIf(x => x.Content != null && !x.Content.IsLocked && x.Content.IsLoaded, x => x.ResizeThumbSize / 2)
            .Else(x => 0d)
            .To((x, v) =>
            {
                x.Log.Debug($"Setting {nameof(x.ResizeBorderThickness)} to {v}");
                x.ResizeBorderThickness = v;
            });
        
        Binder.BindIf(x => x.Content != null, x => !x.Content.IsLocked || x.Content.IsFocusable)
            .Else(x => false)
            .To(x => x.IsFocusable);
    }
    
    public OverlayWindowContainer(IFluentLog logger) : base(logger)
    {
        Binder.Attach(this).AddTo(Anchors);
    }
    
    public new IOverlayViewModel Content
    {
        get => base.Content;
        set => base.Content = (T) value;
    }
    
    protected override void FormatToString(ToStringBuilder builder)
    {
        base.FormatToString(builder);
        var content = Content;
        if (content != null)
        {
            builder.AppendParameter(nameof(Content.OverlayMode), content.OverlayMode);
        }
    }
}

public sealed class OverlayWindowContainer : OverlayWindowContainer<IOverlayViewModel>
{
    public OverlayWindowContainer(IFluentLog logger) : base(logger)
    {
    }
}