using JetBrains.Annotations;

namespace PoeShared.Native
{
    public interface IStringMatcher
    {
        bool IsMatch([CanBeNull] string value);
    }
}