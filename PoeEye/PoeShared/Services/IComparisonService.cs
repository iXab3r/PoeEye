using KellermanSoftware.CompareNetObjects;

namespace PoeShared.Services;

public interface IComparisonService
{
    ComparisonResult Compare(object first, object second);
}