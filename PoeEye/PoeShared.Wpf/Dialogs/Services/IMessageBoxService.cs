using System.Threading.Tasks;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Scaffolding; 

namespace PoeShared.Dialogs.Services;

public interface IMessageBoxService : IDisposableReactiveObject
{
    bool IsOpen { get; }
    
    Task<T> ShowDialog<T>(IMessageBoxViewModel<T> content);

    Task<MessageBoxElement> ShowDialog(string title, object content, params MessageBoxElement[] buttons);
    
    Task ShowMessage(string title, string content);
        
    Task<bool> ShowConfirmation(
        string title,
        string content);
        
    Task<string> ShowInputBox(
        string title,
        string content,
        string contentHint);

    Task<(MessageBoxElement DialogResult, string InputContent)> ShowInputBox(
        string title,
        string content,
        string contentHint,
        bool isReadOnly,
        params MessageBoxElement[] buttons
    );
}