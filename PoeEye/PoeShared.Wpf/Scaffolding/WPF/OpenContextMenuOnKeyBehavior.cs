using System.Windows;
using System.Windows.Input;

namespace PoeShared.Scaffolding.WPF;

public sealed class OpenContextMenuOnKeyBehavior : OpenContextMenuBehavior
{
    public static readonly DependencyProperty KeyProperty = DependencyProperty.Register(
        "Key", typeof(Key), typeof(OpenContextMenuOnKeyBehavior), new PropertyMetadata(default(Key)));

    public Key Key
    {
        get { return (Key) GetValue(KeyProperty); }
        set { SetValue(KeyProperty, value); }
    }    
    
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.PreviewKeyDown += AssociatedObjectOnPreviewKeyDown;
    }

    private void AssociatedObjectOnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key)
        {
            e.Handled = true;
            OpenContextMenu();
        }
    }
}