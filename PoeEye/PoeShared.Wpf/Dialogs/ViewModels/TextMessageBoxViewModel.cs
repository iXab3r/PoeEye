using PoeShared.Native;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.Dialogs.ViewModels
{
    internal sealed class TextMessageBoxViewModel : MessageBoxViewModelBase, ITextMessageBoxViewModel
    {
        private string content;
        private string contentHint;
        private bool isReadOnly;

        public TextMessageBoxViewModel(IClipboardManager clipboardManager) 
        {
            CopyAllCommand = CommandWrapper.Create(() => clipboardManager.SetText(Content));
        }
        
        public CommandWrapper CopyAllCommand { get; }
        
        public string ContentHint
        {
            get => contentHint;
            set => RaiseAndSetIfChanged(ref contentHint, value);
        }

        public string Content
        {
            get => content;
            set => RaiseAndSetIfChanged(ref content, value);
        }

        public bool IsReadOnly
        {
            get => isReadOnly;
            set => RaiseAndSetIfChanged(ref isReadOnly, value);
        }
    }
}