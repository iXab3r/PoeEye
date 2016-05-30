using Newtonsoft.Json;

namespace PoeEye.ExileToolsApi.Extensions
{
    internal static class JsonStringExtensions
    {
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
    }
}