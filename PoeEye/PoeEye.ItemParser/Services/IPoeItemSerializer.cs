using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeEye.ItemParser.Services {
    public interface IPoeItemSerializer
    {
        [CanBeNull] 
        string Serialize([NotNull] IPoeItem item);
    }
}