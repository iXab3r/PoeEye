using System;
using System.Collections.Generic;

namespace PoeShared.Modularity
{
    internal interface IPoeConfigConverterMigrationService
    {
        bool TryGetConverter(Type targetType, int sourceVersion, int targetVersion, out KeyValuePair<PoeConfigMigrationConverterKey, Func<object, object>> result);
    }
}