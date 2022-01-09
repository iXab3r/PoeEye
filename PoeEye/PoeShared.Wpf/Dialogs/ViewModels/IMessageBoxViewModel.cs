using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.ViewModels;

public interface IMessageBoxViewModel : IDisposableReactiveObject
{
    bool CloseOnClickAway { get; }
}

public interface IMessageBoxViewModel<T> : IMessageBoxViewModel, ICloseable<T>
{
}