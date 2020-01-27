using JetBrains.Annotations;

namespace PoeShared.Native
{
    public interface IRegexStringMatcher : IStringMatcher
    {
        [NotNull] 
        IRegexStringMatcher AddToBlacklist(string regexValue);
        
        [NotNull] 
        IRegexStringMatcher AddToWhitelist(string regexValue);

        [NotNull] 
        IRegexStringMatcher ClearWhitelist();
    }
}