using System.Text.RegularExpressions;

namespace PoeShared.Services;

public static class StringMatcher
{
    public static bool MatchExpression(string input, string expression)
    {
        if (expression == null || input == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(expression) && string.IsNullOrEmpty(input))
        {
            return true;
        }
        expression = expression.Trim(' ', '\t');
        var isRegexExpression = expression.IsSurroundedWith("/");
        var isExactExpression = expression.IsSurroundedWith("\"") || expression.IsSurroundedWith("'");
        if (isExactExpression || isRegexExpression)
        {
            expression = expression.Trim('\"', '\'', '/');
        }

        if (isRegexExpression)
        {
            return MatchRegex(input, expression);
        }

        return MatchString(input, expression, isExactExpression);
    }
        
    public static bool MatchRegex(string input, string regex)
    {
        if (regex == null || input == null)
        {
            return false;
        }

        return Regex.IsMatch(input, regex, RegexOptions.IgnoreCase);
    }

    public static bool MatchString(string input, string needle, bool exactMatch)
    {
        if (needle == null || input == null)
        {
            return false;
        }
        return exactMatch 
            ? input.Equals(needle, StringComparison.OrdinalIgnoreCase)
            : input.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}