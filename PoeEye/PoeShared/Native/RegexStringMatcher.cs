using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Common.Logging;
using Guards;
using PoeShared.Scaffolding;

namespace PoeShared.Native
{
    public sealed class RegexStringMatcher : IStringMatcher
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RegexStringMatcher));
        private readonly ConcurrentDictionary<string, Regex> blacklist = new ConcurrentDictionary<string, Regex>();

        private readonly ConcurrentDictionary<string, Regex> whitelist = new ConcurrentDictionary<string, Regex>();

        private Func<string> lazyWhiteListRegex;

        public bool IsMatch(string value)
        {
            if (value == null)
            {
                Log.Trace("Provided Value is not set, resetting to default - empty string");
                value = string.Empty;
            }

            if (Log.IsTraceEnabled)
            {
                Log.Trace(
                    $"Matching value '{value}', whitelist: {whitelist.DumpToTextRaw()}, blacklist: {blacklist.DumpToTextRaw()}, lazyWhite: '{lazyWhiteListRegex?.Invoke()}'");
            }

            var isInBlackList = blacklist.Any() && blacklist.Values.Any(x => x.IsMatch(value));
            if (isInBlackList)
            {
                if (Log.IsTraceEnabled)
                {
                    Log.Trace($"Value '{value}' was found in blacklist: {blacklist.DumpToTextRaw()}");
                }

                return false;
            }

            var isInWhiteList = whitelist.Any() && whitelist.Values.Any(x => x.IsMatch(value));

            if (isInWhiteList)
            {
                if (Log.IsTraceEnabled)
                {
                    Log.Trace($"Value '{value}' was found in whitelist: {whitelist.DumpToTextRaw()}");
                }

                return true;
            }

            if (lazyWhiteListRegex != null)
            {
                var lazyRegexRaw = lazyWhiteListRegex();
                var regex = ConstructRegex(lazyRegexRaw);

                if (regex.IsMatch(value))
                {
                    if (Log.IsTraceEnabled)
                    {
                        Log.Trace($"Value '{value}' was found in lazy whitelist: '{lazyRegexRaw}'");
                    }

                    return true;
                }

                if (Log.IsTraceEnabled)
                {
                    Log.Trace($"Failed to match value '{value}' in lazy whitelist: '{lazyRegexRaw}'");
                }
            }

            return false;
        }

        public RegexStringMatcher WithLazyWhitelistItem(Func<string> regexValue)
        {
            Guard.ArgumentNotNull(regexValue, nameof(regexValue));

            lazyWhiteListRegex = regexValue;
            return this;
        }

        public RegexStringMatcher AddToBlacklist(string regexValue)
        {
            Guard.ArgumentNotNull(regexValue, nameof(regexValue));

            blacklist[regexValue] = ConstructRegex(regexValue);
            return this;
        }

        public RegexStringMatcher AddToWhitelist(string regexValue)
        {
            Guard.ArgumentNotNull(regexValue, nameof(regexValue));

            whitelist[regexValue] = ConstructRegex(regexValue);
            return this;
        }

        private Regex ConstructRegex(string regexValue)
        {
            return new Regex(regexValue, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}