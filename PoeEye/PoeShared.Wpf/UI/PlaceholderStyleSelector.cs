using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PoeShared.UI;

public sealed class PlaceholderStyleSelector : StyleSelector
{
    public Style PlaceholderTemplate { get; set; }
    public Style DefaultTemplate { get; set; }

    public override Style SelectStyle(object item, DependencyObject container)
    {
        if (item == CollectionView.NewItemPlaceholder)
        {
            return PlaceholderTemplate;
        }
        return DefaultTemplate ?? base.SelectStyle(item, container);
    }
}