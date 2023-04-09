using PoeShared.Logging;
using PoeShared.Native;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Dialogs.Views;

internal sealed class MessageBoxContainerViewModel : WindowContainerBase<IWindowViewModel>
{
    private static readonly Binder<MessageBoxContainerViewModel> Binder = new();

    static MessageBoxContainerViewModel()
    {
    }

    public MessageBoxContainerViewModel(IFluentLog logger) : base(logger)
    {
        IsFocusable = true;
        Binder.Attach(this).AddTo(Anchors);
    }
}