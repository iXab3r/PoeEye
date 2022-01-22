using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace PoeShared.Converters;

public class CompositeCollectionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var compositeCollection = new CompositeCollection();
        foreach (var value in values)
        {
            if (value is IEnumerable enumerable)
            {
                compositeCollection.Add(new CollectionContainer { Collection = enumerable });
            }
            else
            {
                compositeCollection.Add(value);
            }
        }

        return compositeCollection;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}