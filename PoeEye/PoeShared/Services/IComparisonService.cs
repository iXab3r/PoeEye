using KellermanSoftware.CompareNetObjects;

namespace PoeShared.Services;

public interface IComparisonService
{
    ComparisonResult Compare(object first, object second);
    
    ComparisonResult Compare(object first, object second, Action<ComparisonConfig> configBuilder);
}