using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.ViewModels;

internal sealed class MessageBoxTextContent : DisposableReactiveObject
{
    public string Hint { get; set; }
    public string Text { get; set; }
    public bool IsReadOnly { get; set; }
}