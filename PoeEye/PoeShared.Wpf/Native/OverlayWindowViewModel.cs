using System.Windows;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowViewModel : DisposableReactiveObject
    {
        private static readonly Binder<OverlayWindowViewModel> Binder = new();

        static OverlayWindowViewModel()
        {
            Binder
                .BindIf(x => x.Content != null && !x.Content.IsLocked, x => 15d)
                .Else(x => 0d)
                .To(x => x.GlassFrameThickness);

            Binder
                .BindIf(x => x.Content != null && !x.Content.IsLocked, x => x.ResizeThumbSize / 2)
                .Else(x => 0d)
                .To(x => x.ResizeBorderThickness);
        }

        public OverlayWindowViewModel()
        {
            Binder.Attach(this).AddTo(Anchors);
        }

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
}