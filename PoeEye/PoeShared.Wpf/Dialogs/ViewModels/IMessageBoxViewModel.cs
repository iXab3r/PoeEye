using System.Collections.ObjectModel;
using PoeShared.Scaffolding;
using PoeShared.Scaffolding.WPF;

namespace PoeShared.Dialogs.ViewModels
{
    public interface IMessageBoxViewModel : IDisposableReactiveObject
    {
        string Title { get; }
        
        MessageBoxElement Result { get; }

        bool IsOpen { get; set; }
        
        CommandWrapper CloseMessageBoxCommand { get; }

        ObservableCollection<MessageBoxElement> AvailableCommands { get; }
    }
}