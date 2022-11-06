using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.Dialogs.ViewModels;

public class MessageBoxViewModel : MessageBoxViewModelBase<MessageBoxElement>
{
    private static readonly Binder<MessageBoxViewModel> Binder = new();

    static MessageBoxViewModel()
    {
    }

    public MessageBoxViewModel()
    {
        Binder.Attach(this).AddTo(Anchors);
    }
    
    public object Content { get; set; }

    public ObservableCollection<MessageBoxElement> Buttons { get; } = new();

    public MessageBoxElement DefaultButton => Buttons.FirstOrDefault(x => x.IsDefault);
}