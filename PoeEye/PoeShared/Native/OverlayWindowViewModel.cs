using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeShared.Native
{
    internal sealed class OverlayWindowViewModel : DisposableReactiveObject
    {
        private bool isVisible;

        public bool IsVisible
        {
            get { return isVisible; }
            set { this.RaiseAndSetIfChanged(ref isVisible, value); }
        }
    }
}