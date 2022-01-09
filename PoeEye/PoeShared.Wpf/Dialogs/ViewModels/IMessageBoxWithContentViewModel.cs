using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Dialogs.ViewModels
{
    internal interface IMessageBoxWithContentViewModel : IMessageBoxHostViewModel
    {
        IMessageBoxViewModel Content { get; }
    }

    internal sealed class MessageBoxHostWithContentViewModelBase : MessageBoxHostViewModelBase, IMessageBoxWithContentViewModel
    {
        private static readonly Binder<MessageBoxHostWithContentViewModelBase> Binder = new();

        static MessageBoxHostWithContentViewModelBase()
        {
            Binder
                .BindIf(x => x .Content != default, x => x.Content.CloseOnClickAway)
                .Else(x => true)
                .To(x => x.CloseOnClickAway);
        }

        public MessageBoxHostWithContentViewModelBase()
        {
            Binder.Attach(this).AddTo(Anchors);
        }

        public IMessageBoxViewModel Content { get; set; }
    }
}