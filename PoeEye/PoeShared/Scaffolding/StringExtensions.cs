using System;
using System.Linq;

namespace PoeShared.Scaffolding
{
    public static class StringExtensions
    {
        /// <summary>
        ///     Split string into substrings using separator string
        ///     Empty items are removed and existing are trimmed
        /// </summary>
        public static string[] SplitTrim(this string str, string separator)
        {
            return str
                   .Split(new[] {separator}, StringSplitOptions.RemoveEmptyEntries)
                   .Select(sub => sub.Trim('\n', '\r', ' '))
                   .ToArray();
        }

        public static int? ToIntOrDefault(this string str)
        {
            int result;

            if (int.TryParse(str, out result))
            {
                return result;
            }

            return null;
        }

        public static decimal? ToDecimalOrDefault(this string str)
        {
            decimal result;

            if (decimal.TryParse(str, out result))
            {
                return result;
            }

            return null;
        }

        public static Uri ToUriOrDefault(this string str)
        {
            Uri result;
            if (!Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out result))
            {
                return default(Uri);
            }

            return result;
        }
    }
}