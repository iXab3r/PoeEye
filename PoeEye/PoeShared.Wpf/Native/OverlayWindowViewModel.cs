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

        private bool showWireframes;
        private double resizeThumbSize;
        private IOverlayViewModel content;
        private DataTemplate contentTemplate;

        public OverlayWindowViewModel()
        {
            this.WhenAnyValue(x => x.Content.IsLocked)
                .Select(x => x ? 0d : 15d)
                .ToProperty(out glassFrameThickness, this, x => x.GlassFrameThickness)
                .AddTo(Anchors);
        }

        public bool ShowWireframes
        {
            get => showWireframes;
            set => this.RaiseAndSetIfChanged(ref showWireframes, value);
        }

        public double ResizeThumbSize
        {
            get => resizeThumbSize;
            set => RaiseAndSetIfChanged(ref resizeThumbSize, value);
        }

        public double GlassFrameThickness => glassFrameThickness.Value;

        public IOverlayViewModel Content
        {
            get => content;
            set => RaiseAndSetIfChanged(ref content, value);
        }

        public DataTemplate ContentTemplate
        {
            get => contentTemplate;
            set => RaiseAndSetIfChanged(ref contentTemplate, value);
        }
    }
}