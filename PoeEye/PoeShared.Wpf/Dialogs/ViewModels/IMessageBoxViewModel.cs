using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.ViewModels;

public interface IMessageBoxViewModel : IWindowViewModel
{
    void Close();
}

public interface IMessageBoxViewModel<T> : IMessageBoxViewModel, ICloseable<T>
{
    T Result { get; }
}
