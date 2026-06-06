using System.Text;

namespace PoeShared.Cli;

internal static class CliCommandLineTokenizer
{
    public static bool TrySplit(string line, out string[] args, out string? error)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuote = false;
        var quoteChar = '\0';

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (inQuote)
            {
                if (c == '\\' && i + 1 < line.Length)
                {
                    current.Append(line[++i]);
                    continue;
                }

                if (c == quoteChar)
                {
                    inQuote = false;
                    continue;
                }

                current.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            if (c is '"' or '\'')
            {
                inQuote = true;
                quoteChar = c;
                continue;
            }

            current.Append(c);
        }

        if (inQuote)
        {
            args = [];
            error = "Unterminated quoted argument.";
            return false;
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        args = tokens.ToArray();
        error = null;
        return true;
    }
}
