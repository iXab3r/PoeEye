namespace PoeShared.Modularity;

internal interface IPoeConfigConverterMigrationService
{
    bool TryGetConverter(Type targetType, int sourceVersion, int targetVersion, out PoeConfigMigrationConverter result);
}