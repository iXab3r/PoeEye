using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeEye.ItemParser.Services
{
    public interface IPoeItemParser
    {
        [CanBeNull]
        IPoeItem Parse([NotNull] string serializedItem);
    }
}