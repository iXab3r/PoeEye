using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

using log4net;
using PoeShared.Logging;
using PoeShared.Scaffolding; 
using PoeShared.Logging;

namespace PoeShared.Native
{
    public sealed class RegexStringMatcher : IRegexStringMatcher
    {
        private static readonly IFluentLog Log = typeof(RegexStringMatcher).PrepareLogger();
        private readonly ConcurrentDictionary<string, Regex> blacklist = new ConcurrentDictionary<string, Regex>();

        private readonly ConcurrentDictionary<string, Regex> whitelist = new ConcurrentDictionary<string, Regex>();

        private Func<string> lazyWhiteListRegex;

        public bool IsMatch(string value)
        {
            if (value == null)
            {
                Log.Debug($"Provided Value is not set, resetting to default - empty string");
                value = string.Empty;
            }

            if (Log.IsDebugEnabled)
            {
                Log.Debug(
                    $"Matching value '{value}', whitelist: {whitelist.DumpToString()}, blacklist: {blacklist.DumpToString()}, lazyWhite: '{lazyWhiteListRegex?.Invoke()}'");
            }

            var isInBlackList = blacklist.Any() && blacklist.Values.Any(x => x.IsMatch(value));
            if (isInBlackList)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Value '{value}' was found in blacklist: {blacklist.DumpToString()}");
                }

                return false;
            }

            var isInWhiteList = whitelist.Any() && whitelist.Values.Any(x => x.IsMatch(value));

            if (isInWhiteList)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Value '{value}' was found in whitelist: {whitelist.DumpToString()}");
                }

                return true;
            }

            if (lazyWhiteListRegex != null)
            {
                var lazyRegexRaw = lazyWhiteListRegex();
                if (string.IsNullOrEmpty(lazyRegexRaw))
                {
                    return false;
                }
                
                var regex = ConstructRegex(lazyRegexRaw);

                if (regex.IsMatch(value))
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug($"Value '{value}' was found in lazy whitelist: '{lazyRegexRaw}'");
                    }

                    return true;
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"Failed to match value '{value}' in lazy whitelist: '{lazyRegexRaw}'");
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

        public IRegexStringMatcher AddToBlacklist(string regexValue)
        {
            Guard.ArgumentNotNull(regexValue, nameof(regexValue));

            blacklist[regexValue] = ConstructRegex(regexValue);
            return this;
        }

        public IRegexStringMatcher AddToWhitelist(string regexValue)
        {
            Guard.ArgumentNotNull(regexValue, nameof(regexValue));

            whitelist[regexValue] = ConstructRegex(regexValue);
            return this;
        }

        public IRegexStringMatcher ClearWhitelist()
        {
            whitelist.Clear();
            return this;
        }


        private Regex ConstructRegex(string regexValue)
        {
            return new Regex(regexValue, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}