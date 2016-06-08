using System.Linq;
using System.Text;
using Microsoft.Practices.ObjectBuilder2;
using Newtonsoft.Json;

namespace PoeEye.ExileToolsApi.Extensions
{
    internal static class JsonStringExtensions
    {
        private const string IndentString = "    ";

        public static bool TryParseJson<T>(this string value, out T result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = default(T);
                return false;
            }

            try
            {
                result = JsonConvert.DeserializeObject<T>(value);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        public static string FormatJson(this string jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return string.Empty;
            }

            var indent = 0;
            var quoted = false;
            var sb = new StringBuilder();
            for (var i = 0; i < jsonString.Length; i++)
            {
                var ch = jsonString[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, ++indent).ForEach(item => sb.Append(IndentString));
                        }
                        break;
                    case '}':
                    case ']':
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, --indent).ForEach(item => sb.Append(IndentString));
                        }
                        sb.Append(ch);
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && jsonString[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            quoted = !quoted;
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!quoted)
                        {
                            sb.AppendLine();
                            Enumerable.Range(0, indent).ForEach(item => sb.Append(IndentString));
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!quoted)
                            sb.Append(" ");
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}