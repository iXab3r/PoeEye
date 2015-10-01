namespace PoeEye.PoeTrade
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;

    using DumpToText;

    using Guards;

    using PoeShared.PoeTrade.Query;

    using TypeConverter;

    internal sealed class PoeQueryConverter : IConverter<IPoeQuery, NameValueCollection>
    {
        public NameValueCollection Convert(IPoeQuery value)
        {
            Guard.ArgumentNotNull(() => value);

            var result = new NameValueCollection();
            foreach (var poeQueryArgument in value.Arguments)
            {
                KeyValuePair<string, string> pair;
                if (poeQueryArgument is IPoeQueryStringArgument)
                {
                    pair = Convert((IPoeQueryStringArgument)poeQueryArgument);
                }
                else if (poeQueryArgument is IPoeQueryFloatArgument)
                {
                    pair = Convert((IPoeQueryFloatArgument)poeQueryArgument);
                }
                else if (poeQueryArgument is IPoeQueryIntArgument)
                {
                    pair = Convert((IPoeQueryIntArgument)poeQueryArgument);
                }
                else if (poeQueryArgument is IPoeQueryModArgument)
                {
                    Convert((IPoeQueryModArgument)poeQueryArgument, result);
                    continue;
                }
                else
                {
                    pair = Convert(poeQueryArgument);
                }
                result.Add(pair.Key, pair.Value);
            }
            return result;
        }

        private KeyValuePair<string, string> Convert(IPoeQueryArgument source)
        {
            return new KeyValuePair<string, string>(source.Name, string.Empty);
        }

        private KeyValuePair<string, string> Convert(IPoeQueryStringArgument source)
        {
            return new KeyValuePair<string, string>(source.Name, source.Value);
        }

        private KeyValuePair<string, string> Convert(IPoeQueryFloatArgument source)
        {
            return new KeyValuePair<string, string>(source.Name, Convert(source.Value));
        }

        private KeyValuePair<string, string> Convert(IPoeQueryIntArgument source)
        {
            return new KeyValuePair<string, string>(source.Name, Convert(source.Value));
        }

        private void Convert(IPoeQueryModArgument source, NameValueCollection values)
        {
            values.Add("mods", source.Name);
            if (source.Excluded)
            {
                values.Add("modexclude", "x");
            }
            else
            {
                values.Add("modexclude", string.Empty);

                var argument = source as IPoeQueryRangeModArgument;
                if (argument != null)
                {
                    values.Add("modmin", Convert(argument.Min));
                    values.Add("modmax", Convert(argument.Max));
                }
                else
                {
                    values.Add("modmin", string.Empty);
                    values.Add("modmax", string.Empty);
                }
            }
        }

        private string Convert(int source)
        {
            return source.ToString();
        }

        private string Convert(float source)
        {
            return source.ToString(CultureInfo.InvariantCulture);
        }

        private string Convert(float? source)
        {
            return source == null ? string.Empty : Convert((float)source);
        }
    }
}