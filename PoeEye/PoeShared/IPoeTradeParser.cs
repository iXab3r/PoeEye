namespace PoeShared
{
    using JetBrains.Annotations;

    public interface IPoeTradeParser
    {
        [NotNull]
        IPoeQueryResult ParseQueryResult([NotNull] string rawHtml);
    }
}