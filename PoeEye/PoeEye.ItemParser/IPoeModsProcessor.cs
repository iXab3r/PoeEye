using JetBrains.Annotations;

namespace PoeEye.ItemParser
{
    internal interface IPoeModsProcessor
    {
        [NotNull]
        PoeModParser[] GetKnownParsers();
    }
}