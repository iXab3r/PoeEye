using JetBrains.Annotations;

namespace PoeShared.Common
{
    public interface IPoeItemParser
    {
        [CanBeNull] 
        IPoeItem Parse([NotNull] string serializedItem);
    }
}