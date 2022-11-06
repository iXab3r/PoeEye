using PoeShared.Native;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Dialogs.ViewModels;

internal sealed class MessageBoxWindowViewModel : WindowViewModelBase
{
    private static readonly Binder<MessageBoxWindowViewModel> Binder = new();

    static MessageBoxWindowViewModel()
    {
        
    }

    public MessageBoxWindowViewModel()
    {
        Binder.Attach(this).AddTo(Anchors);
    }
}