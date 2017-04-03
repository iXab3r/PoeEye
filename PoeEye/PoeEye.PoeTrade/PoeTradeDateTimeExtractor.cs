namespace PoeEye.PoeTrade
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Guards;

    using JetBrains.Annotations;

    using PoeShared;

    internal sealed class PoeTradeDateTimeExtractor : IPoeTradeDateTimeExtractor
    {
        private readonly IClock clock;

        private readonly Tuple<Regex, Func<double, TimeSpan>>[] conversions = {
            new Tuple<Regex, Func<double, TimeSpan>>(new Regex("(.*) seconds? ago", RegexOptions.Compiled | RegexOptions.IgnoreCase), TimeSpan.FromSeconds), 
            new Tuple<Regex, Func<double, TimeSpan>>(new Regex("(.*) minutes? ago", RegexOptions.Compiled | RegexOptions.IgnoreCase), TimeSpan.FromMinutes), 
            new Tuple<Regex, Func<double, TimeSpan>>(new Regex("(.*) hours? ago", RegexOptions.Compiled | RegexOptions.IgnoreCase), TimeSpan.FromHours), 
            new Tuple<Regex, Func<double, TimeSpan>>(new Regex("(.*) days? ago", RegexOptions.Compiled | RegexOptions.IgnoreCase), TimeSpan.FromDays),
            new Tuple<Regex, Func<double, TimeSpan>>(new Regex("yesterday", RegexOptions.Compiled | RegexOptions.IgnoreCase), x => TimeSpan.FromDays(1)),
        };

        public PoeTradeDateTimeExtractor([NotNull] IClock clock)
        {
            Guard.ArgumentNotNull(clock, nameof(clock));
            
            this.clock = clock;
        }

        public DateTime? ExtractTimestamp(string timestamp)
        {
            var offset = ExtractOffset(timestamp);
            if (offset == null)
            {
                return null;
            }

            return clock.Now - offset.Value;
        }

        private TimeSpan? ExtractOffset(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
            {
                return null;
            }

            var matches = conversions
                .Select(x => new {Match = x.Item1.Match(timestamp), ConversionFunction = x.Item2})
                .Where(x => x.Match.Success)
                .ToArray();

            if (matches.Length == 0 || matches.Length > 1)
            {
                return null;
            }

            var conversion = matches.Single();
            if (conversion.Match.Groups.Count > 1)
            {
                double argument;
                if (double.TryParse(conversion.Match.Groups[1].Value, out argument))
                {
                    return conversion.ConversionFunction(argument);
                }

                return null;
            }

            return conversion.ConversionFunction(double.NaN);
        }
    }
}
