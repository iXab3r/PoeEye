using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowViewModel : DisposableReactiveObject
    {
        private bool isVisible;

        private bool showWireframes;

        public bool IsVisible
        {
            get { return isVisible; }
            set { this.RaiseAndSetIfChanged(ref isVisible, value); }
        }

        public bool ShowWireframes
        {
            get { return showWireframes; }
            set { this.RaiseAndSetIfChanged(ref showWireframes, value); }
        }

        public IReactiveList<IOverlayViewModel> Items { get; } = new ReactiveList<IOverlayViewModel>();
    }
}