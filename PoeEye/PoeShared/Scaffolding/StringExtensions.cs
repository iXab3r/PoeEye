namespace PoeShared.Scaffolding
{
    using System;
    using System.Linq;

    public static class StringExtensions
    {
        /// <summary>
        ///     Split string into substrings using separator string
        ///     Empty items are removed and existing are trimmed
        /// </summary>
        public static string[] SplitTrim(this string str, string separator)
        {
            return str
                .Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(sub => sub.Trim('\n','\r',' '))
                .ToArray();
        }
    }
}