﻿using PoeShared.Scaffolding.WPF;

namespace PoeShared.Dialogs.ViewModels
{
    internal interface ITextMessageBoxViewModel : IMessageBoxViewModel
    {
        string Content { get; }
        
        string ContentHint { get; }
        
        bool IsReadOnly { get; }
        
        CommandWrapper CopyAllCommand { get; }
    }
}