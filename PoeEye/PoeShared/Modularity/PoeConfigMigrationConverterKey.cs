namespace PoeShared.Modularity;

internal sealed record PoeConfigMigrationConverterKey
{
    public Type SourceType { get; set; }

    public int SourceVersion { get; set; }

    public Type TargetType { get; set; }

    public int TargetVersion { get; set; }

    /// <summary>
    ///  Implicit conversion is used when direct conversion (e.g. from v1 to v3) is not registered but we have v1 to v2 to v3, in this case conversion v1=>v2=>v3 is registered as implicit automatically
    /// </summary>
    public bool IsImplicit { get; set; }
}