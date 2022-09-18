using System.Globalization;
using System.IO.Compression;
using System.Text;


namespace PoeShared.Scaffolding;

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

    public static string TakeMidChars(this string str, int maxChars, bool addSuffix = false)
    {
        if (str == null || str.Length <= maxChars)
        {
            return str;
        }

        var suffix = addSuffix ? $" ({maxChars}+{str.Length - maxChars} chars)" : string.Empty;

        var right = maxChars / 2;
        var left = maxChars - right;
        return str[..left] + "..." + str[^right..] + suffix;
    }
    
    public static string TakeChars(this string str, int maxChars)
    {
        if (str == null || str.Length <= maxChars)
        {
            return str;
        }

        return str[..maxChars] + $"... ({maxChars}+{str.Length - maxChars} chars)";
    }

    public static string JoinStrings(this IEnumerable<string> obj, char separator)
    {
        return string.Join(separator, obj);
    }
        
    public static string JoinStrings(this IEnumerable<string> obj, string separator)
    {
        return string.Join(separator, obj);
    }
        
    public static bool IsSurroundedWith(this string input, string value)
    {
        return input != null && value != null && input.StartsWith(value) && input.EndsWith(value);
    }

    public static string SurroundWith(this string input, char c)
    {
        return c + input + c;
    }
        
    public static string SurroundWith(this string input, string value)
    {
        return value + input + value;
    }

    public static int? ToIntOrDefault(this string str)
    {
        int result;

        if (string.IsNullOrEmpty(str))
        {
            return null;
        }
            
        if (str.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase) ||
            str.StartsWith("&H", StringComparison.CurrentCultureIgnoreCase)) 
        {
            str = str.Substring(2);
            if (int.TryParse(str, NumberStyles.HexNumber, null, out result))
            {
                return result;
            }
        }

        if (int.TryParse(str, out result))
        {
            return result;
        }

        return null;
    }

    /// <summary>
    ///     Perform a string split that also trims whitespace from each result and removes duplicats
    /// </summary>
    /// <param name="text"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static IEnumerable<string> SplitTrim(this string text, char separator)
    {
        var separators = new[] {separator};
        return text.SplitTrim(separators);
    }

    /// <summary>
    ///     Perform a string split that also trims whitespace from each result and removes duplicats
    /// </summary>
    /// <param name="text"></param>
    /// <param name="separator"></param>
    /// <returns></returns>
    public static IEnumerable<string> SplitTrim(this string text, char[] separator)
    {
        var list = (text ?? string.Empty).Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (list.Length <= 0)
        {
            yield break;
        }

        var uniqueList = new HashSet<string>();
        foreach (var item in list)
        {
            if (uniqueList.Add(item))
            {
                yield return item.Trim();
            }
        }
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

    /// <summary>
    ///     By default, pascalize converts strings to UpperCamelCase also removing underscores
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string Pascalize(this string input)
    {
        return input.Length > 0 ? input.Substring(0, 1).ToUpper() + input.Substring(1) : input;
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