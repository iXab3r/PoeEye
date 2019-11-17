using System.Windows;
using System.Windows.Controls;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class StackPanelEx : StackPanel
    {
        public static readonly DependencyProperty HidePartialItemsProperty = DependencyProperty.Register(
            "HidePartialItems", typeof(bool), typeof(StackPanelEx), new PropertyMetadata(default(bool)));

        public bool HidePartialItems
        {
            get => (bool) GetValue(HidePartialItemsProperty);
            set => SetValue(HidePartialItemsProperty, value);
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            if (!HidePartialItems)
            {
                return base.ArrangeOverride(arrangeBounds);
            }

            double currentY = 0;
            foreach (UIElement child in InternalChildren)
            {
                var nextY = currentY + child.DesiredSize.Height;
                if (nextY > arrangeBounds.Height)
                {
                    child.Arrange(new Rect());
                }
                else
                {
                    child.Arrange(new Rect(0, currentY, child.DesiredSize.Width, child.DesiredSize.Height));
                }

                currentY = nextY;
            }

            return arrangeBounds;
        }
    }
}