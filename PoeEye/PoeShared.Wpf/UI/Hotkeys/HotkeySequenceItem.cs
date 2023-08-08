using PoeShared.Scaffolding;
using PropertyBinder;

namespace PoeShared.UI;

public abstract record HotkeySequenceItem : DisposableReactiveRecord
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
}