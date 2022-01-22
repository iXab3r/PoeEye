using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using log4net;
using PoeShared.Scaffolding; 
using PoeShared.Logging;
using PoeShared.Scaffolding.WPF;
using ReactiveUI;

namespace PoeShared.Dialogs.ViewModels;

internal abstract class MessageBoxHostViewModelBase : DisposableReactiveObject, IMessageBoxHostViewModel, ICloseController
{
    private static readonly IFluentLog Log = typeof(MessageBoxHostViewModelBase).PrepareLogger();

    public MessageBoxHostViewModelBase()
    {
        CloseMessageBoxCommand = CommandWrapper.Create<MessageBoxElement?>(x =>
        {
            Result = x ?? default;
            Close();
        });
    }

    public CommandWrapper CloseMessageBoxCommand { get; }

    public string Title { get; set; }

    public bool IsOpen { get; set; }
        
    public bool CloseOnClickAway { get; protected set; }

    public MessageBoxElement Result { get; private set; }
        
    public ObservableCollection<MessageBoxElement> AvailableCommands { get; } = new();

    public MessageBoxElement DefaultCommand => AvailableCommands.FirstOrDefault();
        
    public void Close()
    {
        IsOpen = false;
    }
}