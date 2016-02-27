namespace PoeShared.PoeTrade
{
    using JetBrains.Annotations;

    internal interface IPoeModsProcessor
    {
        [NotNull]
        PoeModParser[] GetKnownParsers();
    }
}