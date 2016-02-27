namespace PoeShared.Common
{
    using JetBrains.Annotations;

    public interface IPoeItemParser
    {
        [CanBeNull]
        IPoeItem Parse([NotNull] string serializedItem);
    }
}