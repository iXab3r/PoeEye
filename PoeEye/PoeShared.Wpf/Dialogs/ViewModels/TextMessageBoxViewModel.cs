using PoeShared.Native;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.Dialogs.ViewModels
{
    internal sealed class TextMessageBoxViewModel : MessageBoxHostViewModelBase, ITextMessageBoxViewModel
    {
        public TextMessageBoxViewModel(IClipboardManager clipboardManager)
        {
            CloseOnClickAway = true;
            CopyAllCommand = CommandWrapper.Create(() =>
            {
                if (!string.IsNullOrEmpty(Content))
                {
                    clipboardManager.SetText(Content);
                }
            });
        }
        
        public CommandWrapper CopyAllCommand { get; }
        
        public string ContentHint { get; set; }

        public string Content { get; set; }

        public bool IsReadOnly { get; set; }
    }
}