using System.Threading.Tasks;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Scaffolding; 

namespace PoeShared.Dialogs.Services
{
    public interface IMessageBoxService : IDisposableReactiveObject
    {
        IMessageBoxHost MessageBox { get; }
        
        public Task<MessageBoxElement> ShowDialog(string title, IMessageBoxViewModel content, params MessageBoxElement[] buttons);
        
        public Task<T> ShowDialog<T>(string title, IMessageBoxViewModel<T> content);
        
        public Task<bool> ShowConfirmation(
            string title,
            string content);
        
        public Task<string> ShowInputBox(
            string title,
            string content,
            string contentHint);

        public Task<(MessageBoxElement DialogResult, string InputContent)> ShowMessageBox(
            string title,
            string content,
            string contentHint,
            bool isReadOnly,
            params MessageBoxElement[] buttons
        );
    }
}