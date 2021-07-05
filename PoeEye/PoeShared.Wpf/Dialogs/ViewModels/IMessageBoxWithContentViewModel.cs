namespace PoeShared.Dialogs.ViewModels
{
    internal interface IMessageBoxWithContentViewModel : IMessageBoxViewModel
    {
        object Content { get; }
    }

    internal sealed class MessageBoxWithContentViewModelBase : MessageBoxViewModelBase, IMessageBoxWithContentViewModel
    {
        private object content;

        public object Content
        {
            get => content;
            set => RaiseAndSetIfChanged(ref content, value);
        }
    }
}