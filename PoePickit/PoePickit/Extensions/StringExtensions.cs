using System;
using System.Globalization;
using Guards;

namespace PoePricer.Extensions
{
    internal static class StringExtensions
    {
        private static readonly CultureInfo PathOfExileCulture = CultureInfo.GetCultureInfo("ru-RU");

        public static int ToInt(this string value)
        {
            Guard.ArgumentNotNull(() => value);

            int result;
            if (!int.TryParse(value, NumberStyles.Any, PathOfExileCulture, out result))
            {
                throw new FormatException($"Failed to convert string to int, value: '{value}'");
            }
            return result;
        }

        public static double ToDouble(this string value)
        {
            Guard.ArgumentNotNull(() => value);

            double result;
            if (!double.TryParse(value, NumberStyles.Any, PathOfExileCulture, out result))
            {
                throw new FormatException($"Failed to convert string to float, value: '{value}'");
            }
            return result;
        }

        public static bool TryParseAsDouble(this string value, out double result)
        {
            return double.TryParse(value, NumberStyles.Any, PathOfExileCulture, out result);
        }

        public static bool TryParseAsInt(this string value, out int result)
        {
            return int.TryParse(value, NumberStyles.Any, PathOfExileCulture, out result);
        }
    }
}