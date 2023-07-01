using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;

namespace PoeShared.Bindings;

internal sealed class PassthroughLinkCustomTypeProvider : IDynamicLinkCustomTypeProvider
{
    private readonly IDynamicLinkCustomTypeProvider fallback = new DynamicLinqCustomTypeProvider();

    public HashSet<Type> GetCustomTypes()
    {
        return fallback.GetCustomTypes();
    }

    public Dictionary<Type, List<MethodInfo>> GetExtensionMethods()
    {
        return fallback.GetExtensionMethods();
    }

    public Type ResolveType(string typeName)
    {
        return fallback.ResolveType(typeName);
    }

    public Type ResolveTypeBySimpleName(string simpleTypeName)
    {
        return fallback.ResolveTypeBySimpleName(simpleTypeName);
    }
}