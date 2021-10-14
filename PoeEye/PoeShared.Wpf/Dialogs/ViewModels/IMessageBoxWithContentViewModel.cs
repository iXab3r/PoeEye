namespace PoeShared.Dialogs.ViewModels
{
    internal interface IMessageBoxWithContentViewModel : IMessageBoxViewModel
    {
        object Content { get; }
    }

    internal sealed class MessageBoxWithContentViewModelBase : MessageBoxViewModelBase, IMessageBoxWithContentViewModel
    {

        public object Content { get; set; }
    }
}