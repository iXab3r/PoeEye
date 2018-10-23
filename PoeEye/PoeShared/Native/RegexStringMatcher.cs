using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using Guards;

namespace PoeShared.Native
{
    public sealed class RegexStringMatcher : IStringMatcher
    {
        private readonly ConcurrentDictionary<string, Regex> whitelist = new ConcurrentDictionary<string, Regex>();
        private readonly ConcurrentDictionary<string, Regex> blacklist = new ConcurrentDictionary<string, Regex>();

        private Func<string> lazyWhiteListRegex;

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
        
        public bool IsMatch(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            
            var isInBlackList = blacklist.Any() && blacklist.Values.Any(x => x.IsMatch(value));
            if (isInBlackList)
            {
                return false;
            }
                
            var isInWhiteList = whitelist.Any() && whitelist.Values.Any(x => x.IsMatch(value));
            isInWhiteList |= lazyWhiteListRegex != null && ConstructRegex(lazyWhiteListRegex()).IsMatch(value);

            return isInWhiteList;
        }
    }
}