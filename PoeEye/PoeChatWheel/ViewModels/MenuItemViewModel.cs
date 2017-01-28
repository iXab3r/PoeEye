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
            get { return text; }
            set { this.RaiseAndSetIfChanged(ref text, value); }
        }

        public string IconText
        {
            get { return iconText; }
            set { this.RaiseAndSetIfChanged(ref iconText, value); }
        }

        public string CommandText
        {
            get { return commandText; }
            set { this.RaiseAndSetIfChanged(ref commandText, value); }
        }
    }
}