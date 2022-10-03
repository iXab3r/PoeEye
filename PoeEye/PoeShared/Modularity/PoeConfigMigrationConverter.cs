namespace PoeShared.Modularity;

internal struct PoeConfigMigrationConverter
{
    public PoeConfigMigrationConverterKey Key { get; set; }
    
    public Func<object, object> Converter { get; set; }
}