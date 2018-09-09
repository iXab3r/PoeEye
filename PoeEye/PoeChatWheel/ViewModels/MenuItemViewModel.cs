using PoeShared.Scaffolding;
using ReactiveUI;

namespace PoeChatWheel.ViewModels
{
    internal sealed class MenuItemViewModel : DisposableReactiveObject
    {
        private string commandText;

        private string iconText;
        private string text;

        public string Text
        {
            get => text;
            set => this.RaiseAndSetIfChanged(ref text, value);
        }

        public string IconText
        {
            get => iconText;
            set => this.RaiseAndSetIfChanged(ref iconText, value);
        }

        public string CommandText
        {
            get => commandText;
            set => this.RaiseAndSetIfChanged(ref commandText, value);
        }
    }
}