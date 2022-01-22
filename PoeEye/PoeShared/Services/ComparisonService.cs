using KellermanSoftware.CompareNetObjects;
using PoeShared.Modularity;

namespace PoeShared.Services;

internal sealed class ComparisonService : IComparisonService
{
    private readonly CompareLogic diffLogic = new CompareLogic(
        new ComparisonConfig
        {
            DoublePrecision = 0.01,
            MaxDifferences = byte.MaxValue,
            AttributesToIgnore = new List<Type> { typeof(ComparisonIgnoreAttribute) },
            CompareStaticFields = false,
            CompareStaticProperties = false,
            DecimalPrecision = 0.01m,
        });
        
    public ComparisonResult Compare(object first, object second)
    {
        var result = diffLogic.Compare(first, second);
        return result;
    }
}