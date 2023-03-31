namespace PoeShared.Modularity;

internal interface IPoeConfigConverterMigrationService
{
    bool AutomaticallyLoadConverters { get; set; }
    
    bool TryGetConverter(Type targetType, int sourceVersion, int targetVersion, out PoeConfigMigrationConverter result);
}