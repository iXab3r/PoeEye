using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.ViewModels;

public interface IMessageBoxHost : IDisposableReactiveObject
{
    bool IsOpen { get; set; }
        
    bool CloseOnClickAway { get; }
}
    
internal interface IMessageBoxHostViewModel : IMessageBoxHost
{
    string Title { get; }
        
    MessageBoxElement Result { get; }
}