using PoeShared.Logging;
using PoeShared.Native;

namespace PoeShared.Dialogs.Views;

internal sealed class MessageBoxContainerViewModel : WindowContainerBase<IWindowViewModel>
{
    public MessageBoxContainerViewModel(IFluentLog logger) : base(logger)
    {
    }
}