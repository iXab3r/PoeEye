using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using PoeShared.Prism;

namespace PoeShared.Communications
{
    internal sealed class NameValueCollectionToQueryStringConverter : IConverter<NameValueCollection, string>,
        IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>
    {
        IEnumerable<KeyValuePair<string, string>> IConverter<NameValueCollection, IEnumerable<KeyValuePair<string, string>>>.Convert(NameValueCollection source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            return ConvertToList(source);
        }

        string IConverter<NameValueCollection, string>.Convert(NameValueCollection source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            var parameters = ConvertToList(source).Select(x => ConvertToParam(x.Key, x.Value));
            return string.Join("&", parameters);
        }

        private IEnumerable<KeyValuePair<string, string>> ConvertToList(NameValueCollection source)
        {
            Guard.ArgumentNotNull(source, nameof(source));

            var idx = 0;
            var needAnotherRun = true;

            var parameters = new List<KeyValuePair<string, string>>();
            while (needAnotherRun)
            {
                needAnotherRun = false;
                foreach (var key in source.AllKeys)
                {
                    var values = source.GetValues(key);
                    if (values == null || values.Length - 1 < idx)
                    {
                        continue;
                    }

                    if (!needAnotherRun)
                    {
                        needAnotherRun = values.Length - 1 > 0;
                    }

                    var value = values[idx];

                    parameters.Add(ConvertToKvp(key, value));
                }

                idx++;
            }

            return parameters;
        }

        private string ConvertToParam(string key, string value)
        {
            return $"{key}={HttpUtility.UrlEncode(value)}";
        }

        private KeyValuePair<string, string> ConvertToKvp(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }
    }
}