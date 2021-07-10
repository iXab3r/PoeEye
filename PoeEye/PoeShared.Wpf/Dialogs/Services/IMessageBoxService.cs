using System.Threading.Tasks;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Dialogs.Services
{
    public interface IMessageBoxService : IDisposableReactiveObject
    {
        IMessageBoxViewModel MessageBox { get; }

        public Task<MessageBoxElement> ShowDialog(string title, object content, params MessageBoxElement[] buttons);
        
        public Task<bool> ShowConfirmation(
            string title,
            string content);

        public Task<(MessageBoxElement DialogResult, string InputContent)> ShowMessageBox(
            string title,
            string content,
            string contentHint,
            bool isReadOnly,
            params MessageBoxElement[] buttons
        );
    }
}