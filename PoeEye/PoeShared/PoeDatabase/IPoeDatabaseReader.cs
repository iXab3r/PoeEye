namespace PoeShared.PoeDatabase
{
    using JetBrains.Annotations;

    public interface IPoeDatabaseReader
    {
        string[] KnownEntitiesNames { [NotNull] get; }
    }
}