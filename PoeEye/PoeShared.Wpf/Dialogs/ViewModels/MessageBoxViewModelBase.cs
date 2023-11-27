using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using PoeShared.Native;
using PoeShared.Scaffolding;

namespace PoeShared.Dialogs.ViewModels;

public abstract class MessageBoxViewModelBase<T> : WindowViewModelBase, IMessageBoxViewModel<T>
{
    protected MessageBoxViewModelBase()
    {
        SizeToContent = SizeToContent.WidthAndHeight;
        MinSize = new WinSize(200, 100);
        ShowInTaskbar = false;
        EnableHeader = false;
        
        WhenKeyDown
            .Where(x => x.Key == Key.Escape)
            .Subscribe(x =>
            {
                x.Handled = true;
                Close();
            })
            .AddTo(Anchors);
        
        CloseCommand = CommandWrapper.Create<T>(x =>
        {
            Result = x;
            Close();
        });
    }
    
    public new string Title
    {
        get => base.Title;
        set => base.Title = value;
    }

    public void Close()
    {
        CloseController?.Close(Result);
    }

    public ICommandWrapper CloseCommand { get; }
    
    public bool CloseOnClickAway { get; [UsedImplicitly] protected set; }
    
    public ICloseController<T> CloseController { get; set; }
    
    public T Result { get; [UsedImplicitly] private set; }
}