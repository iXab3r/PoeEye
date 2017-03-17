using JetBrains.Annotations;
using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowViewModel : DisposableReactiveObject
    {
        private bool showWireframes;

        public bool ShowWireframes
        {
            get { return showWireframes; }
            set { this.RaiseAndSetIfChanged(ref showWireframes, value); }
        }

        public IOverlayViewModel Content { [CanBeNull] get; [CanBeNull] set; }
    }
}