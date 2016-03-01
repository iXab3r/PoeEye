﻿namespace PoeEye.PoeTrade.Communications
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Web;

    using Guards;

    using TypeConverter;

    internal sealed class NameValueCollectionToQueryStringConverter : IConverter<NameValueCollection, string>
    {
        public string Convert(NameValueCollection source)
        {
            Guard.ArgumentNotNull(() => source);

            var idx = 0;
            var needAnotherRun = true;

            var parameters = new List<string>();
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
                    var valueEncoded = HttpUtility.UrlEncode(value);
                    parameters.Add(Convert(key, valueEncoded));
                }
                idx++;
            }

            return string.Join("&", parameters);
        }

        private string Convert(string key, string value)
        {
            return $"{key}={value}";
        }
    }
}