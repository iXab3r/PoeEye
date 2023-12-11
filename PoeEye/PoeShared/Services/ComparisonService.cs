using Force.DeepCloner;
using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;
using PoeShared.Modularity;

namespace PoeShared.Services;

internal sealed class ComparisonService : IComparisonService
{
    private readonly ComparisonConfig diffLogicConfig = new ComparisonConfig
    {
        DoublePrecision = 0.01,
        MaxDifferences = byte.MaxValue,
        AttributesToIgnore = new List<Type> {typeof(ComparisonIgnoreAttribute)},
        CompareStaticFields = false,
        CompareStaticProperties = false,
        DecimalPrecision = 0.01m,
        CustomComparers = new List<BaseTypeComparer>()
        {
            //FIXME There is a bug in CompareNetObjects comparison service, indexers of Numerics namespace are not supported as there are multiple
            // e.g. Cannot compare objects with more than one indexer for object Matrix3x2Value at KellermanSoftware.CompareNetObjects.TypeComparers.PropertyComparer.IsValidIndexer(ComparisonConfig config, PropertyEntity info, String breadCrumb)
            new GenericTypeComparer<System.Numerics.Vector2>(RootComparerFactory.GetRootComparer()),
            new GenericTypeComparer<System.Numerics.Vector3>(RootComparerFactory.GetRootComparer()),
            new GenericTypeComparer<System.Numerics.Matrix3x2>(RootComparerFactory.GetRootComparer()),
            new GenericTypeComparer<System.Numerics.Matrix4x4>(RootComparerFactory.GetRootComparer()),
            new GenericTypeComparer<System.Numerics.Vector4>(RootComparerFactory.GetRootComparer()),
            new GenericTypeComparer<System.Numerics.BigInteger>(RootComparerFactory.GetRootComparer()),
            new GenericTypeComparer<System.Numerics.Quaternion>(RootComparerFactory.GetRootComparer()),
            new GenericTypeComparer<System.Numerics.Plane>(RootComparerFactory.GetRootComparer()),
        }
    };

    private readonly CompareLogic diffLogic;

    public ComparisonService()
    {
        diffLogic = new CompareLogic(diffLogicConfig);
    }

    public ComparisonResult Compare(object first, object second)
    {
        var result = diffLogic.Compare(first, second);
        return result;
    }

    public ComparisonResult Compare(object first, object second, Action<ComparisonConfig> configBuilder)
    {
        var config = diffLogicConfig.DeepClone();
        configBuilder(config);
        var diff = new CompareLogic(config);
        return diff.Compare(first, second);
    }
}