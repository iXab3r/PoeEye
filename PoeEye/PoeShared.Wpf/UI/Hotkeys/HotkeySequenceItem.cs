using System;
using System.Windows.Input;
using Force.DeepCloner;
using PoeShared.Scaffolding;
using PropertyBinder;
using ReactiveUI;

namespace PoeShared.UI;

public abstract class HotkeySequenceItem : DisposableReactiveObject, ICloneable
{
    private static readonly Binder<HotkeySequenceItem> Binder = new();

    static HotkeySequenceItem()
    {
        Binder.BindIf(x => !x.IsSelected || !x.IsFocused, x => false)
            .To(x => x.IsInEditMode);
    }

    protected HotkeySequenceItem()
    {
        Binder.Attach(this).AddTo(Anchors);
    }

    public bool IsSelected { get; set; }
    
    public bool IsFocused { get; set; }
    
    public bool IsInEditMode { get; set; } = false;
    
    public object Clone()
    {
        return this.DeepClone();
    }
}