using PoeShared.Native;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.Dialogs.ViewModels
{
    internal sealed class TextMessageBoxViewModel : MessageBoxViewModelBase, ITextMessageBoxViewModel
    {

        public TextMessageBoxViewModel(IClipboardManager clipboardManager) 
        {
            CopyAllCommand = CommandWrapper.Create(() => clipboardManager.SetText(Content));
        }
        
        public CommandWrapper CopyAllCommand { get; }
        
        public string ContentHint { get; set; }

        public string Content { get; set; }

        public bool IsReadOnly { get; set; }
    }
}