namespace PoeEyeUi.Utilities.FilterExtension
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;

    internal sealed class PropertyFilter : DependencyObject, IFilter
    {
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.Register("PropertyName", typeof (string), typeof (PropertyFilter), new UIPropertyMetadata(null));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof (object), typeof (PropertyFilter), new UIPropertyMetadata(null));

        public static readonly DependencyProperty RegexPatternProperty =
            DependencyProperty.Register("RegexPattern", typeof (string), typeof (PropertyFilter), new UIPropertyMetadata(null));

        public string PropertyName
        {
            get { return (string) GetValue(PropertyNameProperty); }
            set { SetValue(PropertyNameProperty, value); }
        }

        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public string RegexPattern
        {
            get { return (string) GetValue(RegexPatternProperty); }
            set { SetValue(RegexPatternProperty, value); }
        }

        public bool Filter(object item)
        {
            if (item == null || string.IsNullOrEmpty(PropertyName))
            {
                return false;
            }

            var type = item.GetType();

            var property = type.GetProperty(PropertyName);
            if (property == null)
            {
                return false;
            }

            var itemValue = property.GetValue(item, null);

            if (string.IsNullOrEmpty(RegexPattern))
            {
                return Equals(itemValue, Value);
            }

            if (itemValue is string == false)
            {
                throw new Exception("Cannot match non-string with regex.");
            }

            var regexMatch = Regex.Match((string) itemValue, RegexPattern);
            return regexMatch.Success;
        }
    }
}