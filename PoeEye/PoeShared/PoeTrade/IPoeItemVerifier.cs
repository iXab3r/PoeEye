using PoeShared.Common;

namespace PoeShared.PoeTrade
{
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    public interface IPoeItemVerifier
    {
        [NotNull]
        Task<bool?> Verify([NotNull] IPoeItem item);
    }
}