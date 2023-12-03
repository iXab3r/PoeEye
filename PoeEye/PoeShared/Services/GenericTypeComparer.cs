using KellermanSoftware.CompareNetObjects;
using KellermanSoftware.CompareNetObjects.TypeComparers;

namespace PoeShared.Services;

internal sealed class GenericTypeComparer<T> : BaseTypeComparer
{
    private static readonly Type ExpectedType = typeof(T);
    private static readonly IEqualityComparer<T> Comparer = EqualityComparer<T>.Default;
    
    public GenericTypeComparer(RootComparer rootComparer) : base(rootComparer)
    {
    }

    public override bool IsTypeMatch(Type type1, Type type2)
    {
        return type1 == ExpectedType && type2 == ExpectedType;
    }

    public override void CompareType(CompareParms parms)
    {
        if (parms.Object1 == null || parms.Object2 == null)
        {
            return;
        }

        if (parms.Object1 is not T value1 || parms.Object2 is not T value2)
        {
            return;
        }

        if (Comparer.Equals(value1, value2))
        {
            return;
        }
        this.AddDifference(parms);
    }
}