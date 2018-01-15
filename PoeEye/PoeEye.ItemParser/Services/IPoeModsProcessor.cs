using JetBrains.Annotations;

namespace PoeEye.ItemParser.Services
{
    internal interface IPoeModsProcessor
    {
        [NotNull]
        PoeModParser[] GetKnownParsers();
    }
}