using JetBrains.Annotations;

namespace PoeShared.Common
{
    public interface IPoeItemParser
    {
        [NotNull] 
        IPoeItem Parse([NotNull] string serializedItem);
    }
}