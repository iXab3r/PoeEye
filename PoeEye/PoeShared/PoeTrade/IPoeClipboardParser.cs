namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;

    using Query;

    public interface IPoeClipboardParser
    {
        [NotNull] 
        IPoeQueryInfo Parse([NotNull] string clipboardContent);
    }
}