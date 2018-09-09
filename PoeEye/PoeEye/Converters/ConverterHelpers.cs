using System.Collections;
using System.Linq;

namespace PoeEye.Converters
{
    internal static class ConverterHelpers
    {
        public static bool IsNullOrEmpty(object value)
        {
            bool isNull;
            if (value is string)
            {
                isNull = IsNull(value as string);
            }
            else if (value is IList)
            {
                isNull = IsNull(value as IList);
            }
            else if (value is IEnumerable)
            {
                isNull = IsNull(value as IEnumerable);
            }
            else
            {
                isNull = value == null;
            }
            return isNull;
        }

        private static bool IsNull(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        private static bool IsNull(IList collection)
        {
            return collection == null || collection.Count <= 0;
        }

        private static bool IsNull(IEnumerable collection)
        {
            return collection == null
                ? true
                : !collection.OfType<object>().Any();
        }
    }
}