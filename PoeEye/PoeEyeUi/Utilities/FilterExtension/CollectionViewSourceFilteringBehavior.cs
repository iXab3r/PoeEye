namespace PoeEyeUi.Utilities.FilterExtension
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Interactivity;
    using System.Windows.Markup;

    [ContentProperty("Filters")]
    internal sealed class CollectionViewSourceFilteringBehavior : Behavior<CollectionViewSource>
    {
        public static readonly DependencyProperty FiltersProperty =
            DependencyProperty.Register("Filters", typeof (IEnumerable<IFilter>), typeof (CollectionViewSourceFilteringBehavior), new UIPropertyMetadata(null));

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public IEnumerable<IFilter> Filters
        {
            get { return (IEnumerable<IFilter>) GetValue(FiltersProperty); }
            set { SetValue(FiltersProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Filter += AssociatedObjectOnFilter;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Filter -= AssociatedObjectOnFilter;
            base.OnDetaching();
        }

        private void AssociatedObjectOnFilter(object sender, FilterEventArgs eventArgs)
        {
            if (Filters == null || eventArgs == null)
            {
                return;
            }

            foreach (var filter in Filters)
            {
                var result = filter.Filter(eventArgs.Item);
                if (result)
                {
                    continue;
                }

                eventArgs.Accepted = false;
                return;
            }

            eventArgs.Accepted = true;
        }
    }
}