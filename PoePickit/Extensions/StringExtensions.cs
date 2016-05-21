namespace PoePickit.Extensions
{
    using System;

    using Guards;

    internal static class StringExtensions
    {
        public static int ToInt(this string value)
        {
            Guard.ArgumentNotNull(() => value);

            int result;
            if (!int.TryParse(value, out result))
            {
                throw new FormatException($"Failed to convert string to int, value: '{value}'");
            }
            return result;
        }

        public static float ToFloat(this string value)
        {
            Guard.ArgumentNotNull(() => value);

            float result;
            if (!float.TryParse(value, out result))
            {
                throw new FormatException($"Failed to convert string to float, value: '{value}'");
            }
            return result;
        }
    }
}