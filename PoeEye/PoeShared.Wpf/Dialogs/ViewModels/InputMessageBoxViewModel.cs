using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.ViewModels;

internal sealed class InputMessageBoxViewModel : DisposableReactiveObject, IMessageBoxViewModel<string>
{
    public InputMessageBoxViewModel()
    {
        ConfirmCommand = CommandWrapper.Create(() =>
        {
            CloseController?.Close(Content);
        });
        CancelCommand = CommandWrapper.Create(() => CloseController?.Close(default));
    }

    public CommandWrapper ConfirmCommand { get; }
    public CommandWrapper CancelCommand { get; }

    public bool IsReadOnly { get; set; }

    public string ContentHint { get; set; }

    public string Content { get; set; }

    public ICloseController<string> CloseController { get; set; }

    public bool CloseOnClickAway => true;
}