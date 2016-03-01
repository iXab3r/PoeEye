namespace PoeShared.PoeTrade
{
    using System.Threading.Tasks;

    using JetBrains.Annotations;

    using PoeShared.Common;

    public interface IPoeItemVerifier
    {
        [NotNull]
        Task<bool?> Verify([NotNull] IPoeItem item);
    }
}