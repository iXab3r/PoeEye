using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PoeShared.UI
{
    internal sealed class PlaceholderTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PlaceholderTemplate { get; set; }
        
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == CollectionView.NewItemPlaceholder)
            {
                return PlaceholderTemplate;
            }
            return base.SelectTemplate(item, container);
        }
    }
}