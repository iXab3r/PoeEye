using System.Threading.Tasks;
using JetBrains.Annotations;
using PoeShared.Common;

namespace PoeShared.PoeTrade
{
    public interface IPoeItemVerifier
    {
        [NotNull]
        Task<bool?> Verify([NotNull] IPoeItem item);
    }
}