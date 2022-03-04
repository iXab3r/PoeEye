using System;
using System.Collections;
using System.Drawing;
using System.Linq;

namespace PoeShared.Converters;

public static class ConverterHelpers
{
    public static bool IsNullOrEmpty(object value)
    {
        if (value == null)
        {
            return true;
        }
            
        return value switch
        {
            int i => IsNullOrDefault(i),
            float f => IsNullOrDefault(f),
            double d => IsNullOrDefault(d),
            decimal m => IsNullOrDefault(m),
            string s => IsNullOrDefault(s),
            DateTime dt => IsNullOrDefault(dt),
            Point p => IsNullOrDefault(p),
            PointF pf => IsNullOrDefault(pf),
            IList list => IsNullOrDefault(list),
            IEnumerable enumerable => IsNullOrDefault(enumerable),
            TimeSpan timeSpan => timeSpan == TimeSpan.Zero,
            Rectangle rectangle => IsNullOrDefault(rectangle),
            _ => false
        };
    }
        
    private static bool IsNullOrDefault<T>(T value) 
    {
        return value == null || value.Equals(default(T));
    }
        
    private static bool IsNullOrDefault(string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    private static bool IsNullOrDefault(IList collection)
    {
        return collection == null || collection.Count <= 0;
    }

    private static bool IsNullOrDefault(IEnumerable collection)
    {
        return collection == null
            ? true
            : !collection.OfType<object>().Any();
    }
}