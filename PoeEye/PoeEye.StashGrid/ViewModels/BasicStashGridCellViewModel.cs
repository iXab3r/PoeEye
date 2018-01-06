using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeEye.StashGrid.ViewModels
{
    public class BasicStashGridCellViewModel : DisposableReactiveObject
    {
        private int height;
        private int left;
        private int top;

        private int width;

        public int Top
        {
            get => top;
            set => this.RaiseAndSetIfChanged(ref top, value);
        }

        public int Left
        {
            get => left;
            set => this.RaiseAndSetIfChanged(ref left, value);
        }

        public int Width
        {
            get => width;
            set => this.RaiseAndSetIfChanged(ref width, value);
        }

        public int Height
        {
            get => height;
            set => this.RaiseAndSetIfChanged(ref height, value);
        }
    }
}