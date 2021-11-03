using JetBrains.Annotations;

namespace PoeShared.Services
{
    public interface IUniqueIdGenerator
    {
        [NotNull]
        string Next();
        
        [NotNull]
        string Next(int length);
    }
}