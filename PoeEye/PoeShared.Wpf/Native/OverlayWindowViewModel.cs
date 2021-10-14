using System.Reactive.Linq;
using System.Windows;
using DynamicData.Annotations;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using ReactiveUI;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowViewModel : DisposableReactiveObject
    {
        private readonly ObservableAsPropertyHelper<double> glassFrameThickness;

        public OverlayWindowViewModel()
        {
            this.WhenAnyValue(x => x.Content.IsLocked)
                .Select(x => x ? 0d : 15d)
                .ToProperty(out glassFrameThickness, this, x => x.GlassFrameThickness)
                .AddTo(Anchors);
        }

        public bool ShowWireframes { get; set; }

        public double ResizeThumbSize { get; set; }

        public double GlassFrameThickness => glassFrameThickness.Value;

        public IOverlayViewModel Content { get; set; }

        public DataTemplate ContentTemplate { get; set; }

        public override string ToString()
        {
            return $"{Content} {Content?.OverlayMode}";
        }
    }
}